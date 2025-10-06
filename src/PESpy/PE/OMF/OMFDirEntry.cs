using System;
using System.Diagnostics;
using ClrDebug.OMF;
using ClrDebug.PDB;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("[{iMod}] {SubSection}")]
    public readonly struct OMFDirEntry : IValue, IViewable
    {
        public SST SubSection => (SST) chunk.PeekUInt16(0);

        public ushort iMod => chunk.PeekUInt16(2);

        public int lfo => chunk.PeekInt32(4);

        public int cb => chunk.PeekInt32(8);

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
            ISymbolAccessor symbolAccessor,
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
            ISymbolAccessor symbolAccessor,
            ref CV_SIGNATURE lastSignature)
        {
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

                    var symbols = new SymTypeList(valueChunk.Pointer, OMFSymHash.StructSize, hash.cbSymbol);
                    //Following this, are the symbol hash and address hash tables. These seem kind of complicated (see cvdump.cpp) so for now we don't include these
                    return new OMFHashedSymbols(hash, symbols);
                }

                case SST.sstGlobalTypes:
                    return new OMFGlobalTypes(valueChunk);

                case SST.sstMPC:
                    throw new NotImplementedException();

                case SST.sstSegMap:
                    return new PDB.OMFSegMap(valueChunk);

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
                case SST.sstFileIndex: //This is the same format as DBI.FileInfo
                    return new OMFFileIndex(valueChunk);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(SubSection), SubSection, sizeof(ushort));
            s.WriteField(nameof(iMod), iMod);
            s.WriteField(nameof(lfo), lfo);
            s.WriteField(nameof(cb), cb);

            return s.ToArray();
        }

        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
