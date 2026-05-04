using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

#if NET
using System.Runtime.InteropServices;
#endif
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /* TPI1 has several methods that can be used to convert between types and type indices:
     * - QueryPbCVRecordForTi (get a TYPTYPE* from a type index)
     * - QueryTiForCVRecord (get a type index from a TYPTYPE*)
     * - QueryTiForUDT
     * 
     * Understanding the inner workings of these methods, and where their data ultimately comes from is quite
     * challenging. Here's how it all works:
     * 
     * 1. When the TPI stream is opened, TPI1::fOpen is called
     * 2. It sees that we already have a TPI stream, and we're reading, so calls fLoad
     * 3. fLoad inspects the header in acslValidateHdr to determine how to proceed next.
     *    For certain older PDBs, it immediately fixes up some bookkeeping. If the file
     *    is being opened in write mode, the existing hash info is eagerly read in. Otherwise,
     *    loading in the data is deferred until you actually attempt to perform some kind of lookup
     * 
     * When you then try and resolve a type index to a TYPTYPE* via QueryPbCVRecordForTi,
     * the next phase of loading occurs
     * 4. QueryPbCVRecordForTi calls fInit to make sure the data has been initialized (if it hasn't been called
     *    previously)
     * 5. fInit calls fInitReally since we haven't been initialized yet
     * 6. fInitReally calculates how many type indices we have (tiMac - tiMin) and resizes mptiprec to be able
     *    to have enough slots to store a PREC for every type index in the PDB, meaning that once mptiprec has been
     *    populated with data, the TYPTYPE* for TI 0x1001 could be resolved by doing mptiprec[1]
     *    
     *    The way that mptiprec gets its data depends on the PDB version in use. In older PDBs, you have to manually
     *    scan the entire TYPTYPE* memory area, measuring the length of each type and storing a pointer to it in each
     *    successive type in mptiprec. For PDBs >= impv40, there is some extra metadata that can make these lookups
     *    a bit faster: whenever TPI1::AddNewTypeRecord is called, it calls TPI1::RecordTiOff, which adds a TI_OFF
     *    record for every 8KB of type data that is written. By comparing the TI of a TI_OFF and the TI_OFF after it,
     *    you can detect the range of memory where a certain subset of type records exist, and load _just those_ records
     *    into mptiprec, rather than being forced to load all of the type information at once
     *    
     * 7. If the impv >= impv40, eagerly read the TI_OFF block regions from disk. TI_OFF is 8 bytes, while TI_OFF_16t
     *    is only 6 (2 byte TI rather than 4). fLoadTiOff reads them all as TI_OFF, and then in the event the data was
     *    actually 16-bit, it clears the junk in the bottom 2 bytes (meaning TI_OFF_16t was padded to 8 bytes anyway?)
     *    
     * 8. For versions where impv < impv70, regardless of whether we have any TI_OFF records or not, fInitReally calls
     *    fInitTiToPrecMap to eagerly construct mptiprec for all type records, ignoring the TI_OFF data that was just read
     *    
     * 9. Finally, fInitReally calls fInitHashToPchnMap which will be used to go in the reverse, from a TYPTYPE* back to a hash
     *    later on
     * 
     * 10. Back in QueryPbCVRecordForTi, we call precForTi. A PREC is a point to a record, which is basically just another name
     *     for a TYPTYPE* (note that PDB 1.0 also uses the term "PREC"). If the PREC for a given TI has been established previously
     *     (either from a prior call to precForTi, or because we eagerly loaded above), mptiprec already contains a PREC for
     *     the given TI index into the array, in which case it returns this PREC immediately. Otherwise, it calls fLoadRecBlk.
     *     
     * 11. fLoadRecBlk searches for a pair of TI_OFF records whose bounds the type index lies within. If no match is found, implicitly
     *     the data must be in the last block. For each record from the start of that block to either the next block (or the maximum
     *     number of type records, if we landed in the last block) it eagerly adds records into mptiprec for the TYPTYPE* records that
     *     that block covers
     * 
     * 12. precForTi then returns the PREC for the type index in mptiprec back to caller, thereby giving them the TYPTYPE*
     * 
     * Note that precForTi also calls fNeeds16bitConversion for all records when it's got a 16-bit pool; this function is not included
     * in microsoft-pdb, but the disassembly shows it just checks for certain leaf indices and calls MapLeaf16To32 on them
     * 
     * Hashing
     * =======
     * The following functions are involved in hashing
     * 
     * hashBufv8     -> SigForPbCb
     * hashBuf       -> HashPbCb
     * hashUdtName   -> hashSz -> LHashPbCb
     * hashTypeIndex -> LHashPbCb
     * hashPrec
     * hashPrecFull
     * 
     * If there are 2500 types in the TPI section, there should be 2500 hash values
     * as well. Each hash denotes an index into a bucket that that type record resolves to.
     * fInitHashToPchnMap initializes mphashpchn with enough space to store cHashBuckets "PCHN"
     * records. PCHN is a simple data structure that represents a linked list node in the chain.
     * You basically just iterate over all type indices, and if a given slot already contains
     * a PCHN in it, create a new node that links to the existing node.
     */

    public static partial class MsfStream
    {
        /// <summary>
        /// Represents the stream pointed to by the sn in <see cref="PESpy.PDB.TpiHash.sn"/>.
        /// </summary>
        public unsafe class TpiHash : IDisposable //This is a stream, not the TpiHash type itself
        {
            //The following information is contained in TpiHash when the header type is "HDR".
            //If the header type is an older type such as HDR_VC50Interim or HDR_16t, this information
            //either comes from the HDR_* data structure directly, or is implied by the version in use.

            private SN sn;
            private int cbHashKey;
            private uint cHashBuckets; //how many buckets we have

            private OffCb offcbHashVals; //offcb of hashvals
            private OffCb offcbTiOff; //offcb of (TI,OFF) pairs
            private OffCb offcbHashAdj; //offcb of hash head list, maps (hashval,ti), where ti is the head of the hashval chain

            private readonly CV_typ_t tiMin;
            private readonly CV_typ_t tiMac;
            private readonly TPIImpv impv;

            private readonly TypTypeList types;

            /* There's a bit of complexity when it comes to this property. microsoft-PDB says it only applies with >= impv41.
             * NT 4 however shows that it applies for impv40+. Furthermore, the scope of what it means to hash
             * UDT records has changed since NT 4. NT 4 only supported fIsGlobalDefnUdt. If you try and hash a type
             * from a vc40 PDB with mspdbcore.dll, it will fail because it will rehash the type record when it upgrades
             * it from 16-bit to 32-bit, but hashPrec doesn't consider the fact that fEnableQueryTiForUdt will be false,
             * thereby blocking it from being able to resolve these records. We can resolve such records just fine however,
             * which seems like a contradiction because NT 4 says the record _should have been_ hashed using its name; yet we
             * were able to resolve it using the CRC32 of its type record instead */
            private bool fEnableQueryTiForUdt;

            public readonly struct TypeAndHash //Like PRECEX
            {
                //Span<T> disallows having a pointer to a struct that itself contains pointers, so we use IntPtr
                //to hide this fact instead
                public TYPTYPE* TypType => (TYPTYPE*) _typType;
                public readonly IntPtr _typType;
                public readonly uint Hash;

                internal TypeAndHash(TYPTYPE* typType)
                {
                    _typType = (nint) typType;
                    Hash = MsfStream.TpiHash.hashPrecFull(typType);
                }
            }

            //TPI1 doesn't just merely store the TYPTYPE* of each record, it also stores the record's associated hash as well. It bundles these items up
            //in an in-memory data structure called PRECEX. It takes a PREC (which is an alias for a TYPTYPE*) and then stores the TYPTYPE* along with its hash
            private MemoryMappedFileHolder? _indexToTypeMapMMF;
            private TypeAndHash* _pIndexToTypeCache;
            private int _numTypeAndHashItems;

            private Span<TypeAndHash> indexToTypeCache => new Span<TypeAndHash>(_pIndexToTypeCache, _numTypeAndHashItems);

            //The hash of a given value gives you a bucket that you're supposed to inspect to get all of the values that collided
            //with the given hash value. That seems like a lot of overhead, so we'll instead store a flat list of a linked list of
            //all of the nodes that share a given collision
            private TpiHashLookup tpiHashLookup;

            private readonly HashDelegate hasher;

            //If impv < impv80
            public NativeSpan<ushort> HashValues16 { get; }

            //impv >= impv80
            public NativeSpan<uint> HashValues32 { get; }

            //Gets the 8KB TI to offset "blocks" that can be used to lazily load
            //the types of a given range of type indices without having to eagerly load
            //all types at once. Not present if < impv40
            public NativeSpan<TI_OFF> TiOff32 { get; }
            public NativeSpan<TI_OFF_16t> TiOff16 { get; }

            //mpnitiHead
            //Used for incremental linking. Note that you're supposed to use this information
            //to insert items at the head of the hash chain. We don't do this, we're only interested
            //in modelling the on-disk format.
            public Map<NI, CV_typ_t, HcNi>? UdtHashAdjustments { get; }

            private PDBFile pdbFile;
            private bool synthetic;

            internal delegate uint HashDelegate(TYPTYPE* TypType, uint cBuckets);

            internal unsafe TpiHash(
                in MemoryChunk chunk,
                in HDR hdr,
                TPIImpv impv,
                TypTypeList types,
                HashDelegate hasher,
                int hashSize)
            {
                var tpiHash = hdr.tpihash;
                this.impv = impv;
                this.types = types;
                this.hasher = hasher;
                this.pdbFile = chunk.PDBFile();

                fEnableQueryTiForUdt = true;

                sn = tpiHash.sn;
                Debug.Assert(tpiHash.snPad == SN.Nil); //I don't ever see this get set anywhere in microsoft-pdb
                cbHashKey = tpiHash.cbHashKey;
                cHashBuckets = (uint) tpiHash.cHashBuckets;
                offcbHashVals = tpiHash.offcbHashVals;
                offcbTiOff = tpiHash.offcbTiOff;
                offcbHashAdj = tpiHash.offcbHashAdj;

                tiMin = hdr.tiMin;
                tiMac = hdr.tiMac;

                if (hashSize == sizeof(int))
                    HashValues32 = chunk.PeekNativeSpan<uint>(offcbHashVals.off, offcbHashVals.cb / sizeof(int));
                else
                    HashValues16 = chunk.PeekNativeSpan<ushort>(offcbHashVals.off, offcbHashVals.cb / sizeof(ushort));

                TiOff32 = chunk.PeekNativeSpan<TI_OFF>(offcbTiOff.off, offcbTiOff.cb / sizeof(TI_OFF));

                //This should only be present in files that have been incrementally linked
                if (offcbHashAdj.cb > 0)
                {
                    //Note that we don't actually apply these adjustments to the head of our hash lists;
                    //we're only interested in capturing what it is on disk
                    UdtHashAdjustments = new Map<NI, CV_typ_t, HcNi>(
                        chunk.Slice(offcbHashAdj.off),
                        c => (CV_typ_t) c.PeekInt32(0),
                        sizeof(CV_typ_t),
                        HcNi.Instance
                    );
                }
            }

            //per tpi.cpp!HDR::operator=(const HDR_16t)
            //HDR_VC50Interim uses the same layout (except its tiMin and tiMac can be 32-bit)
            internal TpiHash(
                in MemoryChunk chunk,
                SN snHash,
                int tiMac,
                int tiMin,
                TPIImpv impv,
                TypTypeList types,
                HashDelegate hasher)
            {
                var hashValsSize = (tiMac - tiMin) * sizeof(ushort);

                this.impv = impv;
                this.types = types;
                this.pdbFile = chunk.PDBFile();

                this.hasher = hasher;

                fEnableQueryTiForUdt = impv >= TPIImpv.impv41;

                sn = snHash;
                cbHashKey = sizeof(ushort);
                cHashBuckets = MsfStream.TPI.cchnV7;
                offcbHashVals = new OffCb
                {
                    off = 0,
                    cb = hashValsSize
                };

                //There's no adjustor section, so the size of the TI_OFF_16t records is the size remaining between the end of the hash values
                //and the end of the stream
                var tiOffSize = (int) (chunk.Remaining - hashValsSize);

                offcbTiOff = new OffCb
                {
                    off = hashValsSize, //i.e. this comes after the data pointed to by offcbHashVals
                    cb = tiOffSize
                };

                //Not present
                offcbHashAdj = new OffCb
                {
                    off = 0,
                    cb = -1
                };

                this.tiMin = tiMin;
                this.tiMac = tiMac;

                var hashValues = chunk.PeekNativeSpan<ushort>(offcbHashVals.off, offcbHashVals.cb / sizeof(ushort));
                HashValues16 = hashValues;

                //Won't be present in vc20
                TiOff16 = chunk.PeekNativeSpan<TI_OFF_16t>(offcbTiOff.off, offcbTiOff.cb / sizeof(TI_OFF_16t));
            }

            //There's no hash info available, so we need to synthesize it
            internal TpiHash(
                PDBFile pdbFile,
                int tiMac,
                int tiMin,
                TPIImpv impv,
                TypTypeList types,
                HashDelegate hasher)
            {
                this.pdbFile = pdbFile;
                this.tiMac = tiMac;
                this.tiMin = tiMin;
                this.types = types;
                this.hasher = hasher;

                cHashBuckets = impv <= TPIImpv.impv70 ? MsfStream.TPI.cchnV7 : MsfStream.TPI.cchnV8;

                fEnableQueryTiForUdt = impv >= TPIImpv.impv41;

                synthetic = true;
            }

            #region GetTypTypeFromIndex

            public TypType GetTypTypeFromIndex(CV_typ_t typeIndex)
            {
                if (!TryGetTypTypeFromIndex(typeIndex, out var typType))
                    throw new InvalidOperationException($"Failed to resolve type index '{typeIndex}'");

                return typType;
            }

            public bool TryGetTypTypeFromIndex(CV_typ_t typeIndex, out TypType typType)
            {
                if (TryGetTypeAndHash(typeIndex, out var typeAndHash))
                {
                    typType = typeAndHash.TypType;
                    return true;
                }

                typType = default;
                return false;
            }

            private bool TryGetTypeAndHash(CV_typ_t typeIndex, out TypeAndHash typeAndHash)
            {
                /* There's 3 possible things need to do
                 * 1. Binary search TiOff32 to get the 32-bit TI_OFF block that this type index lies within,
                 *    and then either linear search (or cache) a mapping between each type index and its corresponding
                 *    TYPTYPE*
                 * 2. Same as above, but using TiOff16 for TI_OFF_16t records
                 * 3. There's no "block" information, so we need to manually construct a mapping between each type index
                 *    and its corresponding TYPTYPE*
                 * 
                 * Strictly speaking, you don't need to have a hash stream in order to be able to map from TI to TYPTYPE*
                 * records; you only need the hash stream to do the reverse. So in the event the hash stream is not present,
                 * we need to structure things such that the PDBFile can still perform type lookups without needing an associated
                 * tpi hash stream. We achieve this by having a "fake" tpi hash stream that the PDBFile constructs when
                 * it's asked to resolve a type and it finds that the hash stream is not present
                 */

                if (typeIndex < tiMin || typeIndex > tiMac)
                {
                    typeAndHash = default;
                    return false;
                }

                switch (impv)
                {
                    case TPIImpv.impv80:
                    case TPIImpv.impv70:
                    case TPIImpv.impv50:
                    case TPIImpv.impv50Interim:
                        //f16bitPool is true if impv <= impv41
                        return TryGetTypeTiOff32(typeIndex, out typeAndHash);

                    case TPIImpv.impv41:
                    case TPIImpv.impv40:
                        return TryGetTypeTiOff16(typeIndex, out typeAndHash);

                    case TPIImpv.intvVC2:
                        //No TI_OFF records, so we need to synthesize a complete table
                        return TryGetTypeDirect(typeIndex, out typeAndHash);

                    default:
                        if (synthetic)
                            return TryGetTypeDirect(typeIndex, out typeAndHash);

                        throw new NotImplementedException($"Don't know how to handle {nameof(TPIImpv)} '{impv}'");
                }
            }

            //If this fails, something has gone catastrophically wrong (meaning we tried to read
            //from the types list but there was nothing left to read!)
            private unsafe bool TryGetTypeTiOff32(CV_typ_t typeIndex, out TypeAndHash typeAndHash)
            {
                //When iterating over all types, it's mega slow having to do the same work over and over, so we will
                //allocate a lookup table to speed things up

                if (indexToTypeCache != null)
                {
                    ref var candidate = ref indexToTypeCache[typeIndex - tiMin];

                    if (candidate.TypType != default)
                    {
                        typeAndHash = candidate;
                        return true;
                    }
                }

                //Find the 8KB block that contains this type index (if no upper bound
                //is found, that means we're in the last block)

                var tiOff = TiOff32;

                var lo = 0;
                var hi = tiOff.Length - 2;

                //The caller must check that the type index is within bounds; if we don't find a better match,
                //implicitly we're in the last block
                var i = tiOff.Length - 1;

                while (lo <= hi)
                {
                    var mid = (lo + hi) / 2;

                    var tiMid = tiOff[mid].ti;
                    var tiNext = tiOff[mid + 1].ti;

                    if (typeIndex < tiMid)
                        hi = mid - 1;
                    else if (typeIndex >= tiNext)
                        lo = mid + 1;
                    else
                    {
                        i = mid;
                        break;
                    }
                }

                var blockLo = tiOff[i];
                var tiHi = i < tiOff.Length - 1 ? tiOff[i + 1].ti : tiMac;

                if (!EagerLoadTypeIndexLookup(blockLo.ti, tiHi, blockLo.off))
                {
                    typeAndHash = default;
                    return false;
                }

                typeAndHash = indexToTypeCache[typeIndex - tiMin];
                Debug.Assert((TYPTYPE*) typeAndHash.TypType != default);

                return true;
            }

            //If this fails, something has gone catastrophically wrong (meaning we tried to read
            //from the types list but there was nothing left to read!)
            private bool TryGetTypeTiOff16(CV_typ_t typeIndex, out TypeAndHash typeAndHash)
            {
                //Same as TryGetType32, just using TI_OFF_16t records instead

                if (indexToTypeCache != null)
                {
                    ref var candidate = ref indexToTypeCache[typeIndex - tiMin];

                    if (candidate.TypType != default)
                    {
                        typeAndHash = candidate;
                        return true;
                    }
                }

                var tiOff = TiOff16;

                var lo = 0;
                var hi = tiOff.Length - 2;

                //The caller must check that the type index is within bounds; if we don't find a better match,
                //implicitly we're in the last block
                var i = tiOff.Length - 1;

                while (lo <= hi)
                {
                    var mid = (lo + hi) / 2;

                    var tiMid = tiOff[mid].ti;
                    var tiNext = tiOff[mid + 1].ti;

                    if (typeIndex < tiMid)
                        hi = mid - 1;
                    else if (typeIndex >= tiNext)
                        lo = mid + 1;
                    else
                    {
                        i = mid;
                        break;
                    }
                }

                var blockLo = tiOff[i];
                var tiHi = i < tiOff.Length - 1 ? (CV_typ_t) (int) tiOff[i + 1].ti : tiMac;

                if (!EagerLoadTypeIndexLookup((CV_typ_t) (int) blockLo.ti, tiHi, blockLo.off))
                {
                    typeAndHash = default;
                    return false;
                }

                typeAndHash = indexToTypeCache[typeIndex - tiMin];
                Debug.Assert((TYPTYPE*) typeAndHash.TypType != default);

                return true;
            }

            //If this fails, something has gone catastrophically wrong (meaning we tried to read
            //from the types list but there was nothing left to read!)
            internal bool TryGetTypeDirect(CV_typ_t typeIndex, out TypeAndHash typeAndHash)
            {
                //Synthesize a mapping from each type index to each TYPTYPE* to use for all type lookups

                if (indexToTypeCache == null)
                    EagerLoadTypeIndexLookup(tiMin, tiMac, 0); //Check for catastrophic failure below so that subsequent calls to resolve types also return false

                typeAndHash = indexToTypeCache[typeIndex - tiMin];

                return typeAndHash.TypType != default;
            }

            private bool EagerLoadTypeIndexLookup(CV_typ_t tiLo, CV_typ_t tiHi, int startOff)
            {
                var enumerator = types.GetEnumerator(startOff);

                var lo = tiLo - tiMin;
                var hi = tiHi - tiMin;

                if (_pIndexToTypeCache == null)
                {
                    var numEntries = tiMac - tiMin;

                    _indexToTypeMapMMF = new MemoryMappedFileHolder(numEntries * sizeof(TypeAndHash));

                    _pIndexToTypeCache = (TypeAndHash*) _indexToTypeMapMMF.Value.Address;
                    _numTypeAndHashItems = numEntries;
                }

                var span = indexToTypeCache;

                for (var i = lo; i < hi; i++)
                {
                    if (!enumerator.MoveNext())
                        return false; //Something has gone catastrophically wrong

                    //The hash of each record is calculated and cached for faster lookups when resolving collisions
                    span[i] = new TypeAndHash(enumerator.Current);
                }

                return true;
            }

            #endregion
            #region GetIndexFromTypType

            //QueryTiForCVRecord
            //Note: when dealing with legacy PDBs, it is not safe to pass types resolved from TPI1 into
            //this method; TPI1 auto-upgrades 16-bit type records to their 32-bit counterparts and rehashes
            //them so that queries work as expected. We attempt to represent the data exactly as it existed on disk,
            //so if a 32-bit type record is passed to us when we were expecting a 16-bit record, we'll fail to resolve
            //the hash
            public bool TryGetIndexFromTypType(TypType typType, out CV_typ_t typeIndex)
            {
                if (tpiHashLookup == null)
                {
                    lock (this)
                    {
                        if (tpiHashLookup == null)
                            InitializeHashLookup();
                    }
                }

                var pdbFile = this.pdbFile;

                var fGlobalDefnUdt = fEnableQueryTiForUdt && fIsGlobalDefnUdt(typType, pdbFile);
                var fLocalDefnUdt = fEnableQueryTiForUdt && fIsLocalDefnUdtWithUniqueName(typType, pdbFile);
                var fUdtSrcLine = fEnableQueryTiForUdt && fIsUdtSrcLine(typType);

                if (fGlobalDefnUdt || fLocalDefnUdt)
                {
                    if (!typType.TryGetName(pdbFile, out var name))
                    {
                        typeIndex = default;
                        return false;
                    }

                    if (fLocalDefnUdt)
                    {
                        //For local UDTs, the hash should be on the unique name. Rather than figure out which
                        //struct to use, you can rely on the fact that the unique name follows the regular name

                        if (name.IsLengthPrefixed)
                            name = new SymString(name.Value + name.Length, true);
                        else
                            name = new SymString(name.Value + name.Length + 1, false);
                    }

                    //QueryTiForCVRecord looks up the name in the name map, but it doesn't actually do anything with the resulting NI
                    //if we're not in write mode, so I don't think we need to do that

                    var hash = hashUdtName(name);

                    return HashAndLookupType(hash, typType, out typeIndex);
                }
                else if (fUdtSrcLine)
                {
                    //Not sure what sizeof(TI) to pass to the hasher when we're 16-bit
                    Debug.Assert(impv == TPIImpv.impv80);

                    //Both lfUdtSrcLine and lfUdtModSrcLine have type as the second member
                    var hash = hashTypeIndex(((LfUdtSrcLine) typType).type);

                    return HashAndLookupType(hash, typType, out typeIndex);
                }
                else
                {
                    var hash = hasher(typType, cHashBuckets);
                    return HashAndLookupType(hash, typType, out typeIndex);
                }
            }

            //TPI1::QueryTiForUDT
            public bool TryGetIndexFromName(SymString name, bool ignoreCase, out CV_typ_t typeIndex)
            {
                if (tpiHashLookup == null)
                {
                    lock (this)
                    {
                        if (tpiHashLookup == null)
                            InitializeHashLookup();
                    }
                }

                if (!fEnableQueryTiForUdt)
                {
                    typeIndex = default;
                    return false;
                }

                var hash = hashUdtName(name);

                var bucket = GetBucket(hash);

                var pdbFile = this.pdbFile;

                foreach (var entry in bucket)
                {
                    if (!TryGetTypeAndHash(entry, out var typeAndHash))
                    {
                        typeIndex = default;
                        return false;
                    }

                    var typType = (TypType) typeAndHash.TypType;

                    if (!fIsGlobalDefnUdt(typType, pdbFile) && !fIsLocalDefnUdtWithUniqueName(typType, pdbFile))
                        continue;

                    var candidateName = typType.GetName(pdbFile);

                    if (fIsLocalDefnUdtWithUniqueName(typType, pdbFile))
                    {
                        if (candidateName.IsLengthPrefixed)
                            candidateName = new SymString(candidateName.Value + candidateName.Length, true);
                        else
                            candidateName = new SymString(candidateName.Value + candidateName.Length + 1, false);
                    }

                    if (ignoreCase)
                    {
                        if (StringHelpers.EqualsIgnoreCase(name.Value, candidateName.Value))
                        {
                            typeIndex = entry;
                            return true;
                        }
                    }

                    if (name.AsSpan().SequenceEqual(candidateName.AsSpan()))
                    {
                        typeIndex = entry;
                        return true;
                    }
                }

                typeIndex = default;
                return false;
            }

            /* The top level method for hashing types in TPI1 is QueryTiForCVRecord. This method then splits off into
             * three separate pathways depending on whether the type is for a UDT, UDT Src Line, or non-UDT type record.
             * Fundamentally however, all 3 of these pathways are the same (the UDT pathway has some extra logic that only
             * applies in ENC mode). The only real difference is the type of hasher that is used. UDT records hash based on
             * the name, UDT Src Line records hash based on the type index of the lfUdtSrcLine symbol, while all others use
             * the default hashing algorithm that applies for the particular TPI version, as determined by the TPI header.
             * As such, we converge all of these 3 pathways together in this method, allowing the caller to pass in the
             * particular hash that they want to use
             * 
             * It's important to note that QueryTiForCVRecord performs _two_ levels of hashing.
             * It hashes the input based on whatever mode should be used (as described above),
             * then it hashes the _TypType itself_ via hashPrecFull. QueryTiForUDT functions
             * very similarly to this method, except it purely hashes based on the input name.
             * It does not apply a second level hash using hashPrecFull. I believe
             * that the purpose of QueryTiForUDT is to facilitate translating forward refs
             * to their primary symbols
             */

            private bool HashAndLookupType(uint hash, TypType typType, out CV_typ_t typeIndex)
            {
                var recHash = hashPrecFull(typType);

                var bucket = GetBucket(hash);

                foreach (var entry in bucket)
                {
                    //TPI1::LookupNonUDTCVRecord aborts immediately if it fails to resolve a TYPTYPE for a TI
                    if (!TryGetTypeAndHash(entry, out var typeAndHash))
                    {
                        typeIndex = default;
                        return false;
                    }

                    //In the case of a UDT, QueryTiForCVRecord calls fSameUDT, updates some bookkeeping specifically related to applying edits,
                    //and then ultimately checks to see whether we're an exact match or not. Since we don't support ENC,
                    //there isn't really any point in calling fSameUDT
                    if (typeAndHash.Hash == recHash && ((TypType) typeAndHash.TypType).AsSpan().SequenceEqual(typType.AsSpan()))
                    {
                        typeIndex = entry;
                        return true;
                    }
                }

                typeIndex = default;
                return false;
            }

            private void InitializeHashLookup()
            {
                /* There's some weird logic in TPI1::fInitHashToPchnMap wherein it doesn't seem
                 * to trust the serialized hash value when the version < impv70. After spending a bit
                 * of time investigating this, I inadvertantly stumbled upon the answer: TPI1
                 * has a mechanism to automatically "upgrade" any 16-bit records it sees to be 32-bit.
                 * When reading an older PDB that contains 16-bit types, these hash values will naturally
                 * be the hashes of their original 16-bit versions. But when TPI1 upgrades these 16-bit
                 * types to be 32-bit, the hashes will no longer match! Thus, it needs to rehash all of
                 * the types.
                 * 
                 * While this explanation sounds good in theory, there's a bit of a wrinkle here: vc40 was the last
                 * version to contain 16-bit types. vc50 and vc70 use 32-bit types, but both still have 16-bit hashes.
                 * If vc70 needed to be rehashed as well that would make sense, but it's a bit odd they only rehash values
                 * _below_ vc70.
                 * 
                 * An important consequence of this 16-bit to 32-bit upgrade logic is that if you try and pass a TYPTYPE
                 * that came from TPI1 to us, we might not be able to resolve the hash, since the upgraded 32-bit version
                 * doesn't exist in our mapping table!
                 */

                /* Using linked lists to store this information would use _way_ too much memory. So my plan
                 * instead is to build a flat collections of items. Each bucket will then get a handle to the collection
                 * of items that belong to it. */

                //fRehashV40ToPchnMap bails out if you're not writing, so it's not relevant to the logic we need to employ here

                //An array of lists
                var buckets = new Dictionary<uint, int>();

                //impv70 and below has 2 byte hashes, impv80 has 4 byte hashes

                if (impv >= TPIImpv.impv80)
                {
                    //32-bit

                    var hashValues = HashValues32;

                    if (hashValues.Length < (tiMac - this.tiMin))
                        throw new NotImplementedException();

                    var tiMin = this.tiMin;

                    for (var i = tiMin; i < tiMac; i++)
                    {
                        var hash = hashValues[i - tiMin];

                        if (!buckets.TryGetValue(hash, out var bucket))
                        {
                            buckets[hash] = 1;
                        }
                        else
                        {
                            buckets[hash] = bucket + 1;
                        }
                    }
                }
                else if (synthetic)
                {
                    var tiMin = this.tiMin;

                    for (var i = tiMin; i < tiMac; i++)
                    {
                        var hash = hashPrec(GetTypTypeFromIndex(i));

                        if (!buckets.TryGetValue(hash, out var bucket))
                        {
                            buckets[hash] = 1;
                        }
                        else
                        {
                            buckets[hash] = bucket + 1;
                        }
                    }
                }
                else //impv < impv80 + not synthetic
                {
                    //16-bit

                    var hashValues = HashValues16;

                    if (hashValues.Length < (tiMac - this.tiMin))
                        throw new NotImplementedException();

                    var tiMin = this.tiMin;

                    for (var i = tiMin; i < tiMac; i++)
                    {
                        var hash = hashValues[i - tiMin];

                        if (!buckets.TryGetValue(hash, out var bucket))
                        {
                            buckets[hash] = 1;
                        }
                        else
                        {
                            buckets[hash] = bucket + 1;
                        }
                    }
                }

                /* The way you're supposed to decompress the dictionary is you're meant to end up with a ginormous array with 0x3ffff slots
                 * in it. Each hash then indexes into a slot, which may or may not contain a value. This uses too much memory, so instead
                 * we use a two level hash based on open addressing, which lets us compress the hash space down to just enough slots
                 * needed to store the hash records. Open addressing is an alternative hashing mode to the more commonly seen "chained"
                 * hashing mode */
                var tpiHashLookup = new TpiHashLookup(buckets, tiMac - tiMin);

                //Now write all the values in
                if (impv >= TPIImpv.impv80)
                {
                    //32-bit

                    var hashValues = HashValues32;

                    var tiMin = this.tiMin;

                    for (var i = tiMin; i < tiMac; i++)
                    {
                        var hash = hashValues[i - tiMin];

                        var handle = tpiHashLookup[hash];

                        var remaining = buckets[hash];
                        var pos = handle.Length - remaining;

                        tpiHashLookup.GetSpan(handle)[pos] = i;

                        buckets[hash] = remaining - 1;
                    }
                }
                else if (synthetic)
                {
                    var tiMin = this.tiMin;

                    for (var i = tiMin; i < tiMac; i++)
                    {
                        var hash = hashPrec(GetTypTypeFromIndex(i));

                        var handle = tpiHashLookup[hash];

                        var remaining = buckets[hash];
                        var pos = handle.Length - remaining;

                        tpiHashLookup.GetSpan(handle)[pos] = i;

                        buckets[hash] = remaining - 1;
                    }
                }
                else //impv < impv80 + not synthetic
                {
                    //16-bit

                    var hashValues = HashValues16;

                    var tiMin = this.tiMin;

                    for (var i = tiMin; i < tiMac; i++)
                    {
                        var hash = hashValues[i - tiMin];

                        var handle = tpiHashLookup[hash];

                        var remaining = buckets[hash];
                        var pos = handle.Length - remaining;

                        tpiHashLookup.GetSpan(handle)[pos] = i;

                        buckets[hash] = remaining - 1;
                    }
                }

                this.tpiHashLookup = tpiHashLookup;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Span<CV_typ_t> GetBucket(uint hash)
            {
                if (!tpiHashLookup.TryGetValue(hash, out var handle))
                    return default;

                return tpiHashLookup.GetSpan(handle);
            }

            private static bool fIsGlobalDefnUdt(TypType typType, PDBFile pdbFile)
            {
                switch (typType.leaf)
                {
                    case LEAF_ENUM_e.LF_ALIAS:
                        return true;

                    //16-bit versions mspdbcore doesn't support
                    case LEAF_ENUM_e.LF_CLASS_16t:
                    case LEAF_ENUM_e.LF_STRUCTURE_16t:
                    case LEAF_ENUM_e.LF_UNION_16t:
                        LfClass16t class16 = (LfClass16t) typType;
                        return !class16.property.fwdref && !class16.property.scoped && !fUDTAnon(typType, pdbFile);

                    case LEAF_ENUM_e.LF_ENUM_16t:
                        //"property" is the 4th field; in lfClass_16t it's 3rd
                        LfEnum16t enum16 = (LfEnum16t) typType;
                        return !enum16.property.fwdref && !enum16.property.scoped && !fUDTAnon(typType, pdbFile);

                    //Older versions mspdbcore doesn't support
                    case LEAF_ENUM_e.LF_CLASS_ST:
                    case LEAF_ENUM_e.LF_STRUCTURE_ST:
                    case LEAF_ENUM_e.LF_UNION_ST:
                    case LEAF_ENUM_e.LF_ENUM_ST:
                    //No LF_INTERFACE_ST

                    case LEAF_ENUM_e.LF_CLASS:
                    case LEAF_ENUM_e.LF_STRUCTURE:
                    case LEAF_ENUM_e.LF_UNION:
                    case LEAF_ENUM_e.LF_ENUM:
                    case LEAF_ENUM_e.LF_INTERFACE:
                        //"property" is the 2nd property in both LfClass and LfEnum. Note that this is not true in the 16-bit version
                        LfClass @class = (LfClass) typType;
                        return !@class.property.fwdref && !@class.property.scoped && !fUDTAnon(typType, pdbFile);

                    //Newer items shown in mspdbcore
                    case LEAF_ENUM_e.LF_CLASS2:
                    case LEAF_ENUM_e.LF_STRUCTURE2:
                    case LEAF_ENUM_e.LF_UNION2:
                    case LEAF_ENUM_e.LF_INTERFACE2:
                    case LEAF_ENUM_e.LF_TAGGED_UNION:
                        throw new NotImplementedException("Handling lfClass2 + tagged union types is not implemented"); //Not sure what to do

                    default:
                        return false;
                }
            }

            private static bool fIsLocalDefnUdtWithUniqueName(TypType typType, PDBFile pdbFile)
            {
                switch (typType.leaf)
                {
                    //16-bit versions mspdbcore doesn't support
                    case LEAF_ENUM_e.LF_CLASS_16t:
                    case LEAF_ENUM_e.LF_STRUCTURE_16t:
                    case LEAF_ENUM_e.LF_UNION_16t:
                        LfClass16t class16 = (LfClass16t) typType;
                        return !class16.property.fwdref && class16.property.scoped && class16.property.hasuniquename && !fUDTAnon(typType, pdbFile);

                    case LEAF_ENUM_e.LF_ENUM_16t:
                        //"property" is the 4th field; in lfClass_16t it's 3rd
                        LfEnum16t enum16 = (LfEnum16t) typType;
                        return !enum16.property.fwdref && enum16.property.scoped && enum16.property.hasuniquename && !fUDTAnon(typType, pdbFile);

                    //Older versions mspdbcore doesn't support
                    case LEAF_ENUM_e.LF_CLASS_ST:
                    case LEAF_ENUM_e.LF_STRUCTURE_ST:
                    case LEAF_ENUM_e.LF_UNION_ST:
                    case LEAF_ENUM_e.LF_ENUM_ST:
                    //No LF_INTERFACE_ST

                    case LEAF_ENUM_e.LF_CLASS:
                    case LEAF_ENUM_e.LF_STRUCTURE:
                    case LEAF_ENUM_e.LF_UNION:
                    case LEAF_ENUM_e.LF_ENUM:
                    case LEAF_ENUM_e.LF_INTERFACE:
                        //"property" is the 2nd property in both LfClass and LfEnum. Note that this is not true in the 16-bit version
                        LfClass @class = (LfClass) typType;
                        return !@class.property.fwdref && @class.property.scoped && @class.property.hasuniquename && !fUDTAnon(typType, pdbFile);

                    //Newer items shown in mspdbcore
                    case LEAF_ENUM_e.LF_CLASS2:
                    case LEAF_ENUM_e.LF_STRUCTURE2:
                    case LEAF_ENUM_e.LF_UNION2:
                    case LEAF_ENUM_e.LF_INTERFACE2:
                    case LEAF_ENUM_e.LF_TAGGED_UNION:
                        //Not sure what the shape of tagged union is, but for the others I believe the difference is that
                        //the count is now the first numeric property, which means all of the other fields are shifted down
                        throw new NotImplementedException("Handling lfClass2 + tagged union types is not implemented"); //Not sure what to do

                    default:
                        return false;
                }
            }

            private static bool fIsUdtSrcLine(TypType typType)
            {
                switch (typType.leaf)
                {
                    case LEAF_ENUM_e.LF_UDT_SRC_LINE:
                    case LEAF_ENUM_e.LF_UDT_MOD_SRC_LINE:
                        return true;

                    default:
                        return false;
                }
            }

            private static bool fUDTAnon(TypType typType, PDBFile pdbFile)
            {
                if (typType.TryGetName(pdbFile, out var name))
                {
                    var span = name.AsSpan();

                    if (IsMatch(span, Strings.unnamedtag) || IsMatch(span, Strings.__unnamed))
                        return true;

                    static bool IsMatch(Span<byte> str1, Span<byte> str2) =>
                        str1.SequenceEqual(str2.Slice(2)) || str1.EndsWith(str2);
                }

                return false;
            }

            #endregion
            #region Hash

            internal static uint hashBufv8(TYPTYPE* typType, uint cBuckets)
            {
                return SigForPbCb((byte*) typType, (uint) typType->len + sizeof(short), 0) % cBuckets;
            }

            internal static uint hashBuf(TYPTYPE* typType, uint cBuckets)
            {
                return Hasher.lhashPbCb((byte*) typType, typType->len + sizeof(short), cBuckets);
            }

            private uint hashUdtName(SymString symString) => hashSz(symString);

            private uint hashSz(SymString symString)
            {
                return Hasher.lhashPbCb(symString.Value, symString.Length, cHashBuckets);
            }

            private uint hashTypeIndex(CV_typ_t typeIndex)
            {
                return Hasher.lhashPbCb((byte*) (&typeIndex), sizeof(int), cHashBuckets);
            }

            private uint hashPrec(TypType typType)
            {
                if (fEnableQueryTiForUdt) //microsoft-pdb doesn't do this check which I think is erroneous
                {
                    if (fIsGlobalDefnUdt(typType, pdbFile))
                        return hashUdtName(typType.GetName(pdbFile));

                    if (fIsLocalDefnUdtWithUniqueName(typType, pdbFile))
                    {
                        var name = typType.GetName(pdbFile);

                        if (name.IsLengthPrefixed)
                            name = new SymString(name.Value + name.Length, true);
                        else
                            name = new SymString(name.Value + name.Length + 1, false);

                        return hashUdtName(name);
                    }

                    if (fIsUdtSrcLine(typType))
                    {
                        var srcLine = (LfUdtSrcLine) typType;
                        return hashTypeIndex(srcLine.type);
                    }
                }

                return hasher(typType, cHashBuckets);
            }

            private static uint hashPrecFull(TYPTYPE* typType)
            {
                return hashBufv8(typType, uint.MaxValue);
            }

            #region CRC32

            private static ReadOnlySpan<uint> rgcrc => new uint[]
            {
                0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F,
                0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988,
                0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91, 0x1DB71064, 0x6AB020F2,
                0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7,
                0x136C9856, 0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
                0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172,
                0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 0x35B5A8FA, 0x42B2986C,
                0xDBBBC9D6, 0xACBCF940, 0x32D86CE3, 0x45DF5C75, 0xDCD60DCF, 0xABD13D59,
                0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423,
                0xCFBA9599, 0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
                0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190, 0x01DB7106,
                0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433,
                0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D,
                0x91646C97, 0xE6635C01, 0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E,
                0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
                0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65,
                0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2, 0x4ADFA541, 0x3DD895D7,
                0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0,
                0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA,
                0xBE0B1010, 0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
                0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81,
                0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A,
                0xEAD54739, 0x9DD277AF, 0x04DB2615, 0x73DC1683, 0xE3630B12, 0x94643B84,
                0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
                0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
                0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC,
                0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 0xD6D6A3E8, 0xA1D1937E,
                0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B,
                0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55,
                0x316E8EEF, 0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
                0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE, 0xB2BD0B28,
                0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D,
                0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F,
                0x72076785, 0x05005713, 0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38,
                0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
                0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777,
                0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69,
                0x616BFFD3, 0x166CCF45, 0xA00AE278, 0xD70DD2EE, 0x4E048354, 0x3903B3C2,
                0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC,
                0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
                0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605, 0xCDD70693,
                0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94,
                0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D
            };

            private static uint SigForPbCb(byte* pb, uint cb, uint sig)
            {
                var arr = rgcrc;

                while (cb-- > 0)
                {
                    sig = (sig >> 8) ^ arr[(int) ((sig & 0xff) ^ *pb++)];
                }
                return sig;
            }

            #endregion
            #endregion

            public void Dispose()
            {
                if (_indexToTypeMapMMF != null)
                {
                    _indexToTypeMapMMF.Value.Dispose();
                    _indexToTypeMapMMF = default;
                }

                if (tpiHashLookup != null)
                {
                    tpiHashLookup.Dispose();
                    tpiHashLookup = null;
                }
            }
        }
    }
}
