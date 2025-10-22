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
    [Source(SourceKind.cvexefmt)]
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

        public ushort iMod => chunk.PeekUInt16(iModOffset);

        public int lfo => chunk.PeekInt32(lfoOffset);

        public int cb => chunk.PeekInt32(cbOffset);

        public object Data { get; }

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
            NB05SymbolAccessor symbolAccessor,
            ref CV_SIGNATURE lastSignature)
        {
            this.chunk = chunk;
            Data = default;
            Data = GetData(SubSection, outerChunk.Slice(lfo), cb, symbolAccessor, ref lastSignature);
        }

        private static unsafe object GetData(
            SST subSection,
            in MemoryChunk valueChunk,
            int length,
            NB05SymbolAccessor symbolAccessor,
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
                            SymbolMemoryTracker.RegisterCVSymbolMemory(valueChunk, symbolAccessor);
                            return new OMFModuleTypes(valueChunk.AbsoluteOffset, signature, new TypTypeList(valueChunk.Pointer + 4, length - 4));

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
                            SymbolMemoryTracker.RegisterCVSymbolMemory(valueChunk, symbolAccessor);
                            return new OMFModuleSymbols(valueChunk, signature, new SymTypeList(valueChunk.Pointer, sizeof(int), length - sizeof(int), symbolAccessor));

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

                    using var libraries = new PooledList<FixedAnsiString>();

                    while (read < length)
                    {
                        var strLen = valueChunk.PeekByte(read);
                        read++;

                        var str = valueChunk.PeekAnsiFixedLength(read, strLen);
                        read += strLen;
                        libraries.Add(str);
                    }

                    return libraries.ToArray();
                }

                case SST.sstGlobalSym:
                case SST.sstGlobalPub:
                case SST.sstStaticSym:
                {
                    var hash = new OMFSymHash(valueChunk);

                    //Don't know what the signature is. If it's OMF data I feel like C13 should be impossible, in which case all strings are length prefixed, so just say it's C11
                    SymbolMemoryTracker.RegisterCVSymbolMemory(valueChunk, symbolAccessor);

                    if (lastSignature == default)
                        lastSignature = CV_SIGNATURE.C11;

                    var symbols = new SymTypeList(valueChunk.Pointer, OMFSymHash.StructSize, hash.cbSymbol, symbolAccessor);

                    var symbolHashTable = valueChunk.PeekNativeSpan<byte>(OMFSymHash.StructSize + hash.cbSymbol, hash.cbHSym);
                    var addressHashTable = valueChunk.PeekNativeSpan<byte>(OMFSymHash.StructSize + hash.cbSymbol + hash.cbHSym, hash.cbHAddr);

                    return new OMFHashedSymbols(hash, symbols, symbolHashTable, addressHashTable);
                }

                case SST.sstGlobalTypes:
                    return new OMFGlobalTypes(valueChunk, length, symbolAccessor);

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

                    return names.ToArray();
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFDirEntry, this, ViewKind.OMFDirEntry, StructSize);

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
