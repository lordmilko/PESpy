using System;
using System.Diagnostics;
using System.Text;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    //NT 4 refers to this as the CV 4.0 dnt/DNT
    [Source(SourceKind.cvexefmt_h)]
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct OMFDirEntry : IValue, IViewable
    {
        private string DebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append($"[{iMod}] {SubSection}");

            switch (SubSection)
            {
                case SST.sstModule:
                    builder.Append(" ").Append(Data.ToString());
                    break;
            }

            return builder.ToString();
        }

        private const int SubSectionOffset = 0;
        private const int iModOffset = 2;
        private const int lfoOffset = 4;
        private const int cbOffset = 8;

        public SST SubSection => (SST) chunk.PeekUInt16(SubSectionOffset);

        //Note that these indices seem to be 1-based
        public ushort iMod => chunk.PeekUInt16(iModOffset);

        public int lfo => chunk.PeekInt32(lfoOffset);

        public int cb => chunk.PeekInt32(cbOffset);

        public IValue Data { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //iMod
            sizeof(int) + //lfo
            sizeof(int); //cb

        private readonly MemoryChunk chunk;

        internal OMFDirEntry(
            in MemoryChunk chunk,
            in MemoryChunk outerChunk,
            NB05SymbolAccessor codeViewAccessor,
            ref CV_SIGNATURE lastSignature)
        {
            this.chunk = chunk;
            Data = default;
            Data = GetData(SubSection, iMod, outerChunk.Slice(lfo), cb, codeViewAccessor, ref lastSignature);
        }

        private static unsafe IValue GetData(
            SST subSection,
            ushort imod,
            in MemoryChunk valueChunk,
            int length,
            NB05SymbolAccessor codeViewAccessor,
            ref CV_SIGNATURE lastSignature)
        {
            /* NT 4 defines several types which I think have been renamed in modern headers (see newdeb.h) for the CV 4.0
             * SST info (which is what NB05 is)
             *
             * dnthdr/DNTHDR
             * dnt/DNT
             * pubinfo16/PUB16
             * pubinfo32/PUB32
             * seginfo/SEGINFO
             * CVSRC
             * CVGSN
             * CVLINE
             */

            switch (subSection)
            {
                case SST.sstModule:
                    return new OMFModule(valueChunk);

                case SST.sstTypes:
                {
                    var signature = (CV_SIGNATURE) valueChunk.PeekInt32(0);

                    switch (signature)
                    {
                        case CV_SIGNATURE.C7:
                        case CV_SIGNATURE.C11:
                            Debug.Assert(lastSignature == default || lastSignature == signature); //We expect all signatures should be the same
                            lastSignature = signature;

                            //ctor registers symbol memory
                            return new OMFModuleTypes(
                                valueChunk,
                                signature,
                                new TypTypeList(valueChunk.Pointer + 4, length - 4),
                                codeViewAccessor
                            );

                        case CV_SIGNATURE.C13:
                        default:
                            throw new NotImplementedException($"Don't know how to handle signature {signature}. We should not be getting C13 in OMF, and C6 does not use OMF");
                    }
                }

                case SST.sstPublic:
                    Debug.Assert(false);
                    return null;

                case SST.sstSymbols:
                case SST.sstPublicSym:
                case SST.sstAlignSym: //Once symbols have been written from an obj file, the sstSymbols section becomes sstAlignSym
                {
                    //I would expect all subsections within a given module to have the same signature
                    var signature = (CV_SIGNATURE) valueChunk.PeekInt32(0);

                    switch (signature)
                    {
                        case CV_SIGNATURE.C7:
                        case CV_SIGNATURE.C11:
                            Debug.Assert(lastSignature == default || lastSignature == signature); //We expect all signatures should be the same
                            lastSignature = signature;
                            
                            //ctor registers symbol memory
                            return new OMFModuleSymbols(
                                valueChunk,
                                imod,
                                signature,
                                new SymTypeList(valueChunk.Pointer, sizeof(int), length, codeViewAccessor),
                                codeViewAccessor
                            );

                        case CV_SIGNATURE.C13:
                        default:
                            throw new NotImplementedException($"Don't know how to handle signature {signature}. We should not be getting C13 in OMF, and C6 does not use OMF");
                    }
                }

                case SST.sstSrcLnSeg:
                    //Based on cvdump.cpp!DumpSrcLn, the format is the same as NB02'S SSTSRCLNSEG, but I'm not sure what struct we should use
                    Debug.Assert(false);
                    return null;

                case SST.sstSrcModule:
                    return new OMFSourceModule(valueChunk);

                case SST.sstLibraries:
                {
                    /* There is a type OMFLibrary defined as follows
                    *
                    *     //  sstLibraries
                    *     typedef struct OMFLibrary {
                    *         unsigned char   cbLibs;     // count of library names
                    *         char            Libs[1];    // array of length prefixed lib names (first entry zero length)
                    *     } OMFLibrary;
                    *
                    * On this basis, we would expect there to be a cbLibs member prior to the list of names, however the spec says that sstLibraries is just a sequence of
                    * length prefixed names https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf (pdf page 80)
                    *
                    * Experimentally, I can confirm that neither NB05 nor NB11 have a cbLibs member at the front of them.
                    *
                    * Even more curiously, in the spec for NB02 we have the following
                    *
                    *     // sstLibraries
                    *     typedef struct {
                    *         unsigned char     cbLibs;
                    *         char              Libs[];
                    *     } lib[];                              // an array of lib names
                    *
                    * What I take this to mean is that a library is simply a length prefixed string, and that sstLibraries is an array
                    * of these items. The OMFLibrary type, therefore, is simply a typedef for a "length prefixed string"
                    */

                    //The first entry is an empty string, because library indices are 1-based
                    var read = 0;

                    using var libraries = new PooledList<SymString>();

                    while (read < length)
                    {
                        var str = valueChunk.PeekSymString(read, isLengthPrefixed: true);
                        read += str.Length + 1;
                        libraries.Add(str);
                    }

                    return new RawValue<SymString[]>(valueChunk.AbsoluteOffset, libraries.ToArray());
                }

                case SST.sstGlobalSym:
                case SST.sstGlobalPub:
                case SST.sstStaticSym:
                {
                    var hash = new OMFSymHash(valueChunk);

                    //Don't know what the signature is. If it's OMF data I feel like C13 should be impossible, in which case all strings are length prefixed, so just say it's C11

                    if (lastSignature == default)
                        lastSignature = CV_SIGNATURE.C11;

                    var symbols = new SymTypeList(valueChunk.Pointer, OMFSymHash.StructSize, hash.cbSymbol, codeViewAccessor);

                    var symbolHashTableChunk = valueChunk.Slice(OMFSymHash.StructSize + hash.cbSymbol);
                    var addressHashTableChunk = valueChunk.Slice(OMFSymHash.StructSize + hash.cbSymbol + hash.cbHSym);

                    IValue symbolHashTable;

                    switch (hash.symhash)
                    {
                        case 0:
                            Debug.Assert(hash.cbHSym == 0);
                            symbolHashTable = null;
                            break;

                        case 2:
                        case 6:
                            symbolHashTable = SymHash32(symbolHashTableChunk, hash.symhash, hash.cbHSym);
                            break;

                        case 10:
                            symbolHashTable = SymHash32Long(symbolHashTableChunk, hash.symhash);
                            break;

                        default:
                            Debug.Assert(false);
                            symbolHashTable = new ByteBlob(symbolHashTableChunk, hash.cbHSym, ViewKind.UnknownSymHash);
                            break;
                    }

                    IValue addressHashTable;

                    //Unlike dumpsym7.cpp, we have a unified algorithm for processing all hash table types
                    switch (hash.addrhash)
                    {
                        case 0:
                            Debug.Assert(hash.cbHAddr == 0);
                            addressHashTable = null;
                            break;

                        case 4:
                        case 5:
                        case 8:
                        case 12:
                            addressHashTable = AddrHash32(addressHashTableChunk, hash.addrhash);
                            break;

                        default:
                            Debug.Assert(false);
                            addressHashTable = new ByteBlob(addressHashTableChunk, hash.cbHAddr, ViewKind.UnknownAddrHash);
                            break;
                    }

                    //ctor registers symbol memory
                    return new OMFHashedSymbols(valueChunk, hash, symbols, symbolHashTable, addressHashTable, codeViewAccessor);
                }

                case SST.sstGlobalTypes:
                    return new OMFGlobalTypes(valueChunk, length, codeViewAccessor);

                case SST.sstMPC:
                    throw new NotImplementedException();

                case SST.sstSegMap:
                    return new OMFSegMap(valueChunk);

                case SST.sstSegName:
                {
                    using var names = new PooledList<AnsiString>();

                    var read = 0;

                    while (read < length)
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(read);
                        read += str.Length + 1;
                        names.Add(str);
                    }

                    return new RawValue<AnsiString[]>(valueChunk.AbsoluteOffset, names.ToArray());
                }

                case SST.sstPreComp:
                case SST.sstPreCompMap:
                case SST.sstOffsetMap16:
                case SST.sstOffsetMap32:
                    Debug.Assert(false);
                    return null;

                case SST.sstFileIndex: //This is the same format as DBI.FileInfo
                    return new OMFFileIndex(valueChunk, length, lastSignature != CV_SIGNATURE.C13);

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(SST)} '{subSection}'");
            }
        }

        private static IValue SymHash32(in MemoryChunk chunk, int symhash, int cbHSym)
        {
            Debug.Assert(symhash == 2); //Have only seen version 2

            var cBuckets = chunk.PeekUInt16(0);
            var pad = chunk.PeekUInt16(2);

            //Buckets
            var buckets = chunk.PeekNativeSpan<int>(4, cBuckets);

            var read = 4 + (sizeof(int) * cBuckets);

            //rgCounts
            var counts = chunk.PeekNativeSpan<ushort>(read, cBuckets);

            read += (sizeof(short) * cBuckets);

            //Chains

            //Each entry is the offset of a symbol. Unlike with SymHash32Long, there is no checksum
            var chains = new NativeSpan<int>[cBuckets];

            for (var i = 0; i < counts.Length; i++)
            {
                var count = counts[i];

                chains[i] = chunk.PeekNativeSpan<int>(read, count);

                read += (count * sizeof(int));
            }

            return new SymHash32(chunk.AbsoluteOffset, symhash, cBuckets, pad, buckets, counts, chains);
        }

        internal static IValue SymHash32Long(in MemoryChunk chunk, int symhash)
        {
            Debug.Assert(symhash == 10); //ViewProvider hardcodes it being 10, so if that's not the case we need to change that

            //dumpsym7.cpp!SymHash32Long and PDF page 85 of
            //https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf

            var cBuckets = chunk.PeekUInt16(0);
            var pad = chunk.PeekUInt16(2);

            //Buckets
            var buckets = chunk.PeekNativeSpan<int>(4, cBuckets);

            var read = 4 + (sizeof(int) * cBuckets);

            //rgCounts
            var counts = chunk.PeekNativeSpan<int>(read, cBuckets);

            read += (sizeof(int) * cBuckets);

            //Chains

            //Each entry is a pair of dwords: the file offset and the checksum of the referenced symbol
            var chains = new NativeSpan<(int symbolOffset, uint checksum)>[cBuckets];

            for (var i = 0; i < counts.Length; i++)
            {
                var count = counts[i];

                chains[i] = chunk.PeekNativeSpan<(int symbolOffset, uint checksum)>(read, count);

                read += (count * (sizeof(int) + sizeof(int)));
            }

            return new SymHash32Long(chunk.AbsoluteOffset, symhash, cBuckets, pad, buckets, counts, chains);
        }

        internal static IValue AddrHash32(in MemoryChunk chunk, int addrhash)
        {
            /* https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
             * PDF page 86, and also dumpsym7.cpp!AddrHash32
             * 
             * Based on my analysis of dumpsym7.cpp!AddrHash32 and AddrHash32NB09, there are essentially 3 major formats for representing the address
             * hash table
             * 
             * 1. 4 byte offset counts, 8 byte offset table entries (type 12)
             * 3. 2 byte offset counts, 8 byte offset table entries (type 8: NB09)
             * 3. 2 byte offset counts, 4 byte offset table entries (types 4 and 5)
             * 
             * dumpsym7.cpp splits the NB09 case out into its own function (AddrHash32NB09).
             * We combine all of the logic in one big function, and then emit the appropriate type of hash
             * table object based on the required format
             */

            var cSeg = chunk.PeekUInt16(0);
            var pad = chunk.PeekUInt16(2);

            //rgulSeg
            var segmentTable = chunk.PeekNativeSpan<int>(4, cSeg);

            var offsetCountsOffset = sizeof(short) + sizeof(short) + (cSeg * sizeof(int));

            /* What follows is now variable:
             * 1. addrhash 12 has 4 byte offset counts, 8 byte offset table records
             * 2. addrhash 8 has 2 byte offset counts, 8 byte offset table records
             * 3. addrhash 5 has 2 byte offset counts, padding if cSeg was odd, 4 byte offset table records
             * 4. addrhash 4 has 2 byte offset counts, 4 byte offset table records
             */

            NativeSpan<ushort> offsetCounts16 = default;
            NativeSpan<int> offsetCounts32 = default;

            NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] offsetTable32 = default;
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] offsetTable16 = default;

            ushort segCountPadding = 0;

            int read;

            switch (addrhash)
            {
                case 4:
                case 5:
                case 8:
                    //2 byte offset counts
                    offsetCounts16 = chunk.PeekNativeSpan<ushort>(offsetCountsOffset, cSeg);
                    read = offsetCountsOffset + (cSeg * sizeof(ushort));
                    break;

                case 12:
                    //4 byte offset counts
                    offsetCounts32 = chunk.PeekNativeSpan<int>(offsetCountsOffset, cSeg);
                    read = offsetCountsOffset + (cSeg * sizeof(int));
                    break;

                default:
                    throw new NotImplementedException();
            }

            //dumpsym7.cpp doesn't even seem to know what this means, but it seems to me that it's saying that when the hash mode is 5,
            //there must be an even number of offset counts, so if the count is odd, apply padding before reading the actual offset table
            if (addrhash == 5 && (cSeg & 1) != 0)
            {
                segCountPadding = chunk.PeekUInt16(read);
                read += sizeof(short);
            }

            //Following this, we then have the offset table, which is essentially a jagged array containing offsetCounts[i] items

            switch (addrhash)
            {
                case 4:
                case 5:
                    offsetTable16 = new NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[cSeg];

                    for (var i = 0; i < cSeg; i++)
                    {
                        var count = offsetCounts16[i];

                        //The first value is the offset of the given symbol in the symbol chunk.
                        //The second value is the section relative offset of that symbol
                        offsetTable16[i] = chunk.PeekNativeSpan<(ushort, ushort)>(read, count);

                        read += (count * (sizeof(ushort) + sizeof(ushort)));
                    }
                    break;

                case 8:
                case 12:
                    offsetTable32 = new NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[cSeg];

                    for (var i = 0; i < cSeg; i++)
                    {
                        var count = offsetCounts32[i];

                        //The first value is the offset of the given symbol in the symbol chunk.
                        //The second value is the section relative offset of that symbol
                        offsetTable32[i] = chunk.PeekNativeSpan<(int, int)>(read, count);

                        read += (count * (sizeof(int) + sizeof(int)));
                    }
                    break;
            }

            switch (addrhash)
            {
                case 4:
                    return new AddrHash32v4(chunk.AbsoluteOffset, cSeg, pad, segmentTable, offsetCounts16, offsetTable16);

                case 5:
                    return new AddrHash32v5(chunk.AbsoluteOffset, cSeg, pad, segmentTable, offsetCounts16, segCountPadding, offsetTable16);

                case 8:
                    return new AddrHash32v8(chunk.AbsoluteOffset, cSeg, pad, segmentTable, offsetCounts16, offsetTable32);

                case 12:
                    return new AddrHash32v12(chunk.AbsoluteOffset, cSeg, pad, segmentTable, offsetCounts32, offsetTable32);

                default:
                    throw new NotImplementedException();
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OMFDirEntry, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(SubSection), SubSectionOffset, SubSection, sizeof(ushort));
                    break;

                case 1:
                    structWriter.WriteField(nameof(iMod), iModOffset, iMod);
                    break;

                case 2:
                    structWriter.WriteField(nameof(lfo), lfoOffset, lfo);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
