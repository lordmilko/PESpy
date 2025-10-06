using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_DebugSSubsectionHeader_t
    public readonly struct CvDebugSSubsectionHeader : IValue, IViewable
    {
        //type
        public DEBUG_S_SUBSECTION_TYPE Type => (DEBUG_S_SUBSECTION_TYPE) chunk.PeekUInt32(0);
        
        //cbLen
        public CV_off32_t Length => chunk.PeekInt32(4);

        public unsafe object Data => GetData<object>();

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Type
            Length;

        private MemoryChunk DataChunk => chunk.Slice(8);

        private readonly MemoryChunk chunk;

        internal CvDebugSSubsectionHeader(in MemoryChunk chunk)
        {
            get
            {
                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS:
                    var symbols = GetSymbols();
                    return Unsafe.As<SymTypeList, T>(ref symbols);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES:
                    var lines = GetLines();
                    return Unsafe.As<CvDebugSLinesHeader, T>(ref lines);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE:
                    var stringTable = GetStringTable();
                    return Unsafe.As<Utf8StringCollection, T>(ref stringTable);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS:
                    var fileChecksums = GetFileChecksums();
                    return Unsafe.As<CvFileCheckSum[], T>(ref fileChecksums);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA:
                    var frameData = GetFrameData();
                    return Unsafe.As<RvaAndFrameData, T>(ref frameData);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES:
                    var inlineeLines = GetInlineeLines();
                    return Unsafe.As<InlineeSigAndLines, T>(ref inlineeLines);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS: //CrossScopeReferences (see DumpModCrossScopeRefs)
                    var crossScopeImports = GetCrossScopeImports();
                    return Unsafe.As<CrossScopeReferencesCollection, T>(ref crossScopeImports);

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS:
                    var crossScopeExports = GetCrossScopeExports();
                    return Unsafe.As<LocalIdAndGlobalIdPairList, T>(ref crossScopeExports);

                                var read = 0;
                                var length = Length;

                                while (read < length)
                                {
                                    var item = new CvFileCheckSum(dataChunk.Slice(read));
                                    read += item.StructSize;

                                    read = (read + 3) & ~3; //Checksums are 32-bit aligned

                                    results.Add(item);
                                }

                                data = results.ToArray();
                                Debug.Assert(read == length);
                            }
                            break;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA:
                            data = new RvaAndFrameData(dataChunk.Slice(8), Length);
                            break;

        public unsafe SymTypeList GetSymbols()
        {
            VerifyType(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS);

            //CV_DebugSSubsectionHeader_t is implicitly C13 data, but we still have to register ourselves in any case

            /* We need to make sure that we register this memory only once. Having the CvDebugSSubsectionHeader type
             * be a class is one way to do that, but that allocates a bunch of memory. So Plan B: have the owner of the memory
             * check whether this chunk has been registered before. This also saves us from doing expensive lookups against
             * the global SymbolMemoryTracker table */

            var dataChunk = DataChunk;

            ISymbolAccessor? symbolAccessor;

            if (dataChunk.block is PagedMemoryBlock block)
            {
                block.PDBFile!.RegisterC13SymbolMemory(dataChunk);
                symbolAccessor = block.PDBFile;
            }
            else
            {
                //OBJ or LIB. We're C13, which means UTF8

                if (dataChunk.block is GlobalMemoryBlock b)
                {
                    var objFile = (OBJFile) b.File;
                    symbolAccessor = objFile.RegisterC13SymbolMemory(dataChunk);
                }
                else
                {
                    var s = (GlobalSubMemoryBlock) dataChunk.block;
                    var member = (LongImportLibraryMember) s.Owner;
                    symbolAccessor = member.RegisterC13SymbolMemory(dataChunk);
                }
            }

            //Don't need to adjust the data + length to account for the header
            return new SymTypeList(dataChunk.Pointer, 0, Length, symbolAccessor);
        }

        public CvDebugSLinesHeader GetLines()
        {
            VerifyType(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES);

            return new CvDebugSLinesHeader(DataChunk, Length);
        }

        public Utf8StringCollection GetStringTable()
        {
            VerifyType(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE);

            var read = 0;

            var end = Length;

                        case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_COFF_SYMBOL_RVA:
                            throw new NotImplementedException();

            var dataChunk = DataChunk;

            while (read < end)
            {
                var str = dataChunk.PeekUtf8NullTerminatedString(read);
                results.Add(new RawValue<Utf8String>(dataChunk.AbsoluteOffset + read, str));
                read += str.Length + 1;
            }
        }

        //I tried to have a non-allocating collection type for this, but it's just too hard working with it,
        //because you need to be able to binary search and index into it all the time and everything. As such,
        //we'll have to settle for not storing the checksum array so we don't balloon our memory usage
        public CvFileCheckSum[] GetFileChecksums()
        {
            VerifyType(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS);

            using var results = new PooledList<CvFileCheckSum>();

            var read = 0;
            var length = Length;

            var dataChunk = DataChunk;

            while (read < length)
            {
                var item = new CvFileCheckSum(dataChunk.Slice(read));
                read += item.StructSize;

                read = (read + 3) & ~3; //Checksums are 32-bit aligned

                results.Add(item);
            }

            Debug.Assert(read == length);
            return results.ToArray();
        }

        private object ParseInlineeLines(in MemoryChunk dataChunk)
        {
            VerifyType(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES);

            var dataChunk = DataChunk;

            var pdbFile = dataChunk.PDBFile();

            pdbFile.RegisterC13SymbolMemory(dataChunk);

            var sig = (CV_INLINEELINES_SIGNATURE) dataChunk.PeekUInt32(0);

            if (sig == CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE)

            var entries = new LocalIdAndGlobalIdPair[Length / LocalIdAndGlobalIdPair.StructSize];

            for (var i = 0; i < entries.Length; i++)
                entries[i] = new LocalIdAndGlobalIdPair(dataChunk.Slice(i * LocalIdAndGlobalIdPair.StructSize));

            return entries;
        }

        private object ParseStringTable(in MemoryChunk dataChunk)
        {
            var read = 0;

            var end = Length;

            using var results = new PooledList<RawValue<Utf8String>>();

            while (read < end)
            {
                var entry = new CrossScopeReferences(dataChunk.Slice(read));

                list.Add(entry);

                read += CrossScopeReferences.FixedStructSize + (entry.countOfCrossReferences * sizeof(int));
            }

            return results.ToArray();
        public LocalIdAndGlobalIdPairList GetCrossScopeExports()
        {
            VerifyType(DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS);

            var dataChunk = DataChunk;

            SymbolMemoryTracker.RegisterPDBSymbolMemory(dataChunk);

            var entries = new LocalIdAndGlobalIdPair[Length / LocalIdAndGlobalIdPair.StructSize];

            for (var i = 0; i < entries.Length; i++)
                entries[i] = new LocalIdAndGlobalIdPair(dataChunk.Slice(i * LocalIdAndGlobalIdPair.StructSize));
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CV_DebugSSubsectionHeader_t, this, ViewKind.CvDebugSSubsectionHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("type", Type, sizeof(int));
            s.WriteField("cbLen", Length);

            switch (Type)
            {
                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_SYMBOLS:
                    if (chunk.block is PagedMemoryBlock p)
                        s.WritePagedValue(chunk.RelativeOffset + 8, p, (SymTypeList) Data);
                    else
                        s.WriteValue(Offset + 8, (SymTypeList) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_LINES:
                    s.WriteInline((CvDebugSLinesHeader) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_STRINGTABLE:
                    s.WriteInlineUtf8NullTerminated((RawValue<Utf8String>[]) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FILECHKSMS:
                    s.WriteInline((CvFileCheckSum[]) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FRAMEDATA:
                    s.WriteInline((RvaAndFrameData) Data);
                    break;

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_INLINEELINES:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEIMPORTS:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_CROSSSCOPEEXPORTS:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_IL_LINES:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_FUNC_MDTOKEN_MAP:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_TYPE_MDTOKEN_MAP:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_MERGED_ASSEMBLYINPUT:
                    throw new NotImplementedException();

                case DEBUG_S_SUBSECTION_TYPE.DEBUG_S_COFF_SYMBOL_RVA:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
