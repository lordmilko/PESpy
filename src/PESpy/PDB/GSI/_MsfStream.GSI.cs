using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using PESpy.View;

namespace PESpy.PDB
{
    public static partial class MsfStream
    {
        public class GSI : IValue, IViewable
        {
            public GSIHashHdr? GsiHdr { get; }

            public NativeSpan<HRFile> HashRecords { get; }

            //Only present in V7
            public NativeSpan<int> BucketsBitmap { get; }

            public NativeSpan<int> BucketOffsets { get; }

            //Gets the in-memory representation of the buckets
            public (int StartIndex, int Count)[] Buckets { get; }

            public GlobalSymTypeList Symbols { get; }

            public int Offset => chunk.AbsoluteOffset;

            private int iphrHash;

            internal readonly MemoryChunk chunk;

            internal unsafe GSI(in MemoryChunk chunk, int length)
            {
                this.chunk = chunk;

                var pdbFile = chunk.PDBFile();

                //I think the hash header, hash records and buckets total cbSymHash. In GSI, the total number of bytes is just the size of the GSI stream

                if (pdbFile.PDB.Features.Contains(PdbFeature.featMinimalDbgInfo))
                    iphrHash = 0x3FFFF;
                else
                    iphrHash = 4096;

                //gsi1::readHash

                var gsiHdr = new GSIHashHdr(chunk);

                if (gsiHdr.verSignature == GSIHashHdr.hdrSignature && gsiHdr.verHdr == GSIHashSCImpv.GSIHashSCImpvV70)
                {
                    GsiHdr = gsiHdr;

                    //microsoft-pdb does does some dodgy stuff loading the HRFile records into an array of HR records,
                    //and then I think it fixes things up, since the layout of HR is not the same as HRFile

                    var numItems = gsiHdr.cbHr / HRFile.StructSize;

                    var hashRecords = chunk.PeekNativeSpan<HRFile>(GSIHashHdr.StructSize, gsiHdr.cbHr / HRFile.StructSize);
                    HashRecords = hashRecords;

                    if (hashRecords.Length > 0)
                    {
                        if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                            throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                        SymbolMemoryTracker.RegisterPDBSymbolMemory(symbolChunk);

                        var symbolsStart = symbolChunk.Pointer;

                        Symbols = new GlobalSymTypeList(hashRecords, symbolsStart);

                        if (gsiHdr.cbBuckets > 0)
                        {
                            /* gsi.cpp contains an #ifdef for SMALLBUCKETS. This define _is_ present in compiled builds of PDB1
                             * (GSI1::ExpandBuckets is present). Even when SMALLBUCKETS is defined, small buckets are only in use
                             * when the two signatures we check above match. Otherwise, we need to use the legacy bucket reading mechanism
                             *
                             * At the beginning of the bucket area is a bitmap describing which buckets in the hashmap have values in them.
                             * Following this bitmap is an array of offsets for each bucket that has a value. In order to know how many entries
                             * there are, we need to know how many bits are set in the bitmap */

                            //GSI1::ExpandBuckets
                            var numBitMapInts = SI.DivideUp(iphrHash + 1, 32);
                            var cbphr = numBitMapInts * sizeof(int);

                            var read = GSIHashHdr.StructSize + gsiHdr.cbHr;

                            //This gives us a bitmap that describes the status of all of the buckets in the hashmap.
                            var bitmap = chunk.PeekNativeSpan<int>(read, numBitMapInts); //Read as int so that we can easily count its bits

                            BucketsBitmap = bitmap;

                            read += cbphr;

                            int numBitsSet = 0;

                            //Now count how many bits are set
                            for (var i = 0; i < bitmap.Length; i++)
                                numBitsSet += CountBits((uint) bitmap[i]);

                            var offsets = chunk.PeekNativeSpan<int>(read, numBitsSet);
                            BucketOffsets = offsets;

                            var offsetIndex = 0;

                            var results = new (int StartIndex, int Count)[iphrHash + 1];

                            for (var i = 0; i <= iphrHash; i++)
                            {
                                /* GSI1::fixHashIn does some insane memory manipulation. On disk, GSI contains a list of HRFile items,
                                 * followed by the bitmap (described above) and then the offsets of each bucket whose value is present.
                                 * What fixHashIn wants to do is
                                 * 1. Convert each HRFile item to an in-memory representation called "HR"
                                 * 2. Make each bucket item point to the in-memory "HR" item that it is associated with
                                 *
                                 * Each HRFile record is 8 bytes, while each HR record is 2x ptr + sizeof(int) bytes (12 bytes in x86).
                                 * Each bucket that contains a value contains a 12-byte offset. PDB1 takes this 12 byte offset, and uses
                                 * it to index into the memory area that is being used by HRFile items, and reinterprets it as a HR item.
                                 * As the buffer containing the HRFile items had an extra HR's worth of data allocated at the front, this
                                 * prevents the data we're reinterpreting as a HR from overwriting a HRFile that we haven't processed yet.
                                 * A pointer to this under construction HR is then stored in the bucket location that originally contained
                                 * the 12 byte offset, and then the actual HRFile that we're up to is retrieved, and stored in the dodgy HR.
                                 * Assigning the HRFile to the HR pointer causes the HRFile to be properly laid out within the HR's memory region
                                 *
                                 * This is way too nuts. All we really do is divide the offset by 12 (which is the size of HROffsetCalc).
                                 * I think that HROffsetCalc is only really used in 64-bit and is needed due to the fact that they want to store
                                 * a pointer, rather than the raw offset itself. We are just storing offsets so we don't need to worry about
                                 * any of this nonsense
                                 */

                                var wordIndex = i >> 5; // divide by 32
                                var bitIndex = i & 31;
                                var isSet = wordIndex < bitmap.Length && ((bitmap[wordIndex] >> bitIndex) & 1) != 0;

                                if (isSet)
                                {
                                    var startIndex = offsets[offsetIndex++] / 12;
                                    int count;

                                    //gsi.cpp does not keep track of the count; they instead create an in-memory linked list
                                    //within GSI1::fixHashIn which uses some mind bending logic that I think ultimately is equivalent
                                    //to the following for knowing how many entries are in each bucket (except they iterate backwards)
                                    if (offsetIndex < offsets.Length - 1)
                                        count = (offsets[offsetIndex] / 12) - startIndex; //We've incremented offsetIndex to the next item
                                    else
                                        count = hashRecords.Length - startIndex;

                                    results[i] = (startIndex, count);
                                }
                                else
                                    results[i] = (-1, 0);
                            }

                            //How many entries are in each bucket? offsets stores a value that when divided by 12
                            //gives us an index into our HashRecords array. This can therefore
                            //indirectly be used to tell us the number of items in a given bucket:
                            //the number of entries is equal to the distance between offsets[i]/12 and offsets[i+1]/12,
                            //or the end of the HashRecords array if i == HashRecords.Length - 1

                            Buckets = results;
                        }
                    }
                    else
                    {
                        Debug.Assert(gsiHdr.cbBuckets == 0); //We assume there's no bucket info to read if we didn't have any records. This may not be true!
                        Symbols = null!;
                    }
                }
                else
                {
                    //There's no header (e.g. it's a PDB2File). First, we have a list of offsets, and then after this we may potentially have some HRFile entries

                    var numBuckets = iphrHash + 1;

                    //How many bytes do the buckets encompass
                    var cbphr = sizeof(int) * numBuckets;

                    //How many bytes do the HRFile records encompass
                    var hrFileLength = length - cbphr;

                    //How many HRFile items are there
                    var numHRFiles = hrFileLength / HRFile.StructSize;

                    //the HRFile's come before the buckets. You can have buckets but have no hash records
                    var hashRecords = chunk.PeekNativeSpan<HRFile>(0, numHRFiles);
                    HashRecords = hashRecords;

                    Debug.Assert(hrFileLength >= 0);

                    var buckets = chunk.PeekNativeSpan<int>(hrFileLength, numBuckets);

                    BucketOffsets = buckets;

                    //The buckets list embedded in the file already contains -1 in every slot
                    //where there is no value. So in order to calculate the length of each item
                    //we need to manually traverse the list

                    var i = 0;

                    var results = new (int StartIndex, int Count)[numBuckets];

                    while (i < buckets.Length)
                    {
                        var value = buckets[i];

                        if (value == -1)
                        {
                            results[i] = (-1, 0);
                            i++;
                        }
                        else
                        {
                            //Find the next item that has a value

                            var oldI = i;
                            var startIndex = value / 12;

                            i++;

                            while (true)
                            {
                                if (i < buckets.Length)
                                {
                                    value = buckets[i];

                                    if (value == -1)
                                    {
                                        results[i] = (-1, 0);
                                        i++;
                                    }
                                    else
                                    {
                                        //Found the next item!
                                        results[oldI] = (startIndex, (value / 12) - startIndex);

                                        //Don't increment i; we'll find the end of _that_ item on the next loop
                                        break;
                                    }
                                }
                                else
                                {
                                    //The last item goes all the way to the end

                                    results[oldI] = (startIndex, hashRecords.Length - startIndex);
                                    break;
                                }
                            }
                        }
                    }

                    Buckets = results;

                    if (hashRecords.Length > 0)
                    {
                        if (!pdbFile.TryGetStreamChunk(pdbFile.DBI!.DbiHdr.snSymRecs, out var symbolChunk))
                            throw new InvalidOperationException("Couldn't retrieve section for DbiHdr.snSymRecs for global symbols");

                        SymbolMemoryTracker.RegisterPDBSymbolMemory(symbolChunk);

                        Symbols = new GlobalSymTypeList(hashRecords, symbolChunk.Pointer);
                    }
                }
            }

            public unsafe bool TryGetSymbol(string name, out SymType symType)
            {
                using var builder = new Utf8StringBuilder(name);

                fixed (byte* p = builder.AsSpan())
                {
                    return TryGetSymbol(new FixedUtf8String(p, builder.Length), out symType);
                }
            }

            //Note that lhashPbCb tolower's the input string, which means this performs
            //a case insensitive lookup
            public unsafe bool TryGetSymbol(FixedUtf8String name, out SymType symType)
            {
                //gsi1::HashSym

                //Note that HashSym has a facility where you can "resume", which enables you to retrieve
                //multiple symbols that match the given name (which is important because the name is hashed
                //case insensitively, so there could be multiple matches)

                var hash = Hasher.lhashPbCb(name.Value, name.Length, (uint) iphrHash);

                var buckets = Buckets;

                if (hash > buckets.Length)
                {
                    symType = default;
                    return false;
                }

                var bucket = buckets[hash];

                var i = 0;

                while (bucket.StartIndex < HashRecords.Length && i < bucket.Count)
                {
                    //This indexes into HashRecords
                    var localSymType = Symbols[bucket.StartIndex + i];

                    var pdbFile = chunk.PDBFile();

                    if (!localSymType.TryGetName(pdbFile, out var symbolName))
                    {
                        symType = default;
                        return false;
                    }

                    var compareResult = ((FixedUtf8String) symbolName).CompareToIgnoreCase(name);

                    if (compareResult == 0)
                    {
                        symType = localSymType;
                        return true;
                    }

                    /* HashSym says that their HR records are sorted by name, so they can bail out
                     * early if the comparison is not less than the target name. I can't see how/where
                     * this sort is occurring, and the on disk symbols are _not_ sorted by name within
                     * a given bucket. So instead, we keep track of the size of each bucket, and just iterate
                     * over all symbols within the bucket to see if we find a match */

                    //Try the next record

                    i++;
                }

                symType = default;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int CountBits(uint u)
            {
                const uint _5_32 = 0x55555555;
                const uint _3_32 = 0x33333333;
                const uint _F1_32 = 0x0f0f0f0f;
                const uint _F2_32 = 0x00ff00ff;
                const uint _F4_32 = 0x0000ffff;

                u -= (u >> 1) & _5_32;
                u = ((u >> 2) & _3_32) + (u & _3_32);
                u = ((u >> 4) & _F1_32) + (u & _F1_32);
                u += u >> 8;
                u += u >> 16;
                return (int) (u & 0xff);
            }

            void IViewable.WriteGlobals(ViewWriter writer) => WriteGlobals(writer);

            IView? IViewable.WriteStruct(ViewWriter writer) => null;

            int IViewable.NumChildren() => throw new NotSupportedException();

            void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

            protected virtual void WriteGlobals(ViewWriter writer)
            {
                if (GsiHdr != null)
                {
                    //If we have a GsiHdr, the following should implicitly be true
                    Debug.Assert(GsiHdr.verSignature == GSIHashHdr.hdrSignature && GsiHdr.verHdr == GSIHashSCImpv.GSIHashSCImpvV70);

                    var offset = chunk.RelativeOffset + GSIHashHdr.StructSize;

                    writer.WritePagedGlobal<HRFile>(offset, (PagedMemoryBlock) chunk.block, HashRecords, ViewKind.HRFile);
                    offset += (HashRecords.Length * HRFile.StructSize);

                    writer.WritePagedGlobal<int>(offset, (PagedMemoryBlock) chunk.block, BucketsBitmap, ViewKind.HashBucketsBitmap);
                    offset += BucketsBitmap.Length * sizeof(int);

                    writer.WritePagedGlobal<int>(offset, (PagedMemoryBlock) chunk.block, BucketOffsets, ViewKind.HashBuckets);
                }
                else
                {
                    writer.WritePagedGlobal<HRFile>(chunk.RelativeOffset, (PagedMemoryBlock) chunk.block, HashRecords, ViewKind.HRFile);
                    writer.WritePagedGlobal<int>(chunk.RelativeOffset + (HashRecords.Length * HRFile.StructSize), (PagedMemoryBlock) chunk.block, BucketOffsets, ViewKind.HashBuckets);
                }
            }
        }
    }
}
