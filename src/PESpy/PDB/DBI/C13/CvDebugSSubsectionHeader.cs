using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;
using PESpy.LIB;
using PESpy.View;
using static ClrDebug.PDB.DEBUG_S_SUBSECTION_TYPE;

//As an aside, there is a method ISymUnmanagedWriter5::MapTokenToSourceSpan; I don't know what
//this actually does; my guess it might just add regular source lines? I _can_ see that a C13
//section writer is created in diasymreader.dll

namespace PESpy.PDB
{
    //CV_DebugSSubsectionHeader_t
    public readonly partial struct CvDebugSSubsectionHeader : IValue, IViewable
    {
        private const int TypeOffset = 0;
        private const int LengthOffset = 4;

        //type
        public DEBUG_S_SUBSECTION_TYPE Type => (DEBUG_S_SUBSECTION_TYPE) chunk.PeekUInt32(TypeOffset);

        //cbLen
        public CV_off32_t Length => chunk.PeekInt32(LengthOffset);

        public unsafe object Data => GetData<object>();

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Type
            + sizeof(int) + //Length
            Length;

        private MemoryChunk DataChunk => chunk.Slice(8);

        private readonly MemoryChunk chunk;

        internal CvDebugSSubsectionHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        /// <summary>
        /// Gets the data that is associated with this subsection. The type of <typeparamref name="T"/>
        /// depends on the <see cref="Type"/> of data contained in the subsection:<para/>
        /// <see cref="DEBUG_S_SYMBOLS"/> = <see cref="SymTypeList"/><para/>
        /// <see cref="DEBUG_S_LINES"/> = <see cref="CvDebugSLinesHeader"/><para/>
        /// <see cref="DEBUG_S_STRINGTABLE"/> = RawValue&lt;Utf8String&gt;[]<para/>
        /// <see cref="DEBUG_S_FILECHKSMS"/> = <see cref="CvFileCheckSum"/>[]<para/>
        /// <see cref="DEBUG_S_FRAMEDATA"/> = <see cref="RvaAndFrameData"/><para/>
        /// <see cref="DEBUG_S_INLINEELINES"/> = <see cref="InlineeSigAndLines"/><para/>
        /// <see cref="DEBUG_S_CROSSSCOPEIMPORTS"/> = <see cref="CrossScopeReferences"/>[]<para/>
        /// <see cref="DEBUG_S_CROSSSCOPEEXPORTS"/> = <see cref="LocalIdAndGlobalIdPair"/>[]<para/>
        /// <see cref="DEBUG_S_IL_LINES"/> = <see cref="CvDebugSLinesHeader"/><para/>
        /// <see cref="DEBUG_S_FUNC_MDTOKEN_MAP"/> = <see cref="FuncMDTokenMap"/><para/>
        /// <see cref="DEBUG_S_TYPE_MDTOKEN_MAP"/> = <see cref="TypeMDTokenMap"/><para/>
        /// <see cref="DEBUG_S_MERGED_ASSEMBLYINPUT"/> = <see cref="MergedAssemblyInfo"/>[]<para/><para/>
        /// Any other subsection types not listed are not supported.
        /// </summary>
        /// <typeparam name="T">The type that corresponds with the <see cref="Type"/> of the subsection</typeparam>
        /// <returns>The data contained in the subsection.</returns>
        public T GetData<T>()
        {
            //If the ignore bit is set, we can't get the data
            if ((Type & DEBUG_S_IGNORE) != 0)
                throw new InvalidOperationException("Can't retrieve data when ignore bit is set");

            switch (Type)
            {
                case DEBUG_S_SYMBOLS:
                    var symbols = GetSymbols();
                    return Unsafe.As<SymTypeList, T>(ref symbols); //SymTypeList is a class so this is safe

                case DEBUG_S_LINES:
                case DEBUG_S_IL_LINES:
                    var lines = GetLines();
                    return Unsafe.As<CvDebugSLinesHeader, T>(ref lines); //CvDebugSLinesHeader is a class so sthis is safe

                case DEBUG_S_STRINGTABLE:
                    var stringTable = GetStringTable();
                    return Unsafe.As<RawValue<Utf8String>[], T>(ref stringTable);

                case DEBUG_S_FILECHKSMS:
                    var fileChecksums = GetFileChecksums();
                    return Unsafe.As<CvFileCheckSum[], T>(ref fileChecksums);

                case DEBUG_S_FRAMEDATA:
                    var frameData = GetFrameData();
                    return Unsafe.As<RvaAndFrameData, T>(ref frameData); //RvaAndFrameData is a class so this is safe

                case DEBUG_S_INLINEELINES:
                    var inlineeLines = GetInlineeLines();
                    return Unsafe.As<InlineeSigAndLines, T>(ref inlineeLines); //InlineeSigAndLines is a class so this is safe

                case DEBUG_S_CROSSSCOPEIMPORTS:
                    var crossScopeImports = GetCrossScopeImports();
                    return Unsafe.As<CrossScopeReferences[], T>(ref crossScopeImports);

                case DEBUG_S_CROSSSCOPEEXPORTS:
                    var crossScopeExports = GetCrossScopeExports();
                    return Unsafe.As<LocalIdAndGlobalIdPair[], T>(ref crossScopeExports);

                case DEBUG_S_FUNC_MDTOKEN_MAP:
                    var funcTokenMap = GetFuncMDTokenMap();

                    if (typeof(T) == typeof(object))
                        return (T) (object) funcTokenMap; //You can't Unsafe.As<> a struct into a box, you'll get weird behavior where it instead returns the Entries field

                    return Unsafe.As<FuncMDTokenMap, T>(ref funcTokenMap);

                case DEBUG_S_TYPE_MDTOKEN_MAP:
                    var typeTokenMap = GetTypeMDTokenMap();

                    if (typeof(T) == typeof(object))
                        return (T) (object) typeTokenMap; //You can't Unsafe.As<> a struct into a box, you'll get weird behavior where it instead returns the Entries field

                    return Unsafe.As<TypeMDTokenMap, T>(ref typeTokenMap);

                case DEBUG_S_MERGED_ASSEMBLYINPUT:
                    var mergedAssemblyInput = GetMergedAssemblyInput();
                    return Unsafe.As<MergedAssemblyInfo[], T>(ref mergedAssemblyInput);

                default:
                    //Haven't found any examples of how to parse DEBUG_S_COFF_SYMBOL_RVA
                    //There are also newer XFG related section kinds that have been introduced since microsoft-pdb
                    //was released, however there is no public information about how to parse these

                    Debug.Assert(false);

                    return default;
            }
        }

        public unsafe SymTypeList GetSymbols()
        {
            VerifyType(DEBUG_S_SYMBOLS);

            //CV_DebugSSubsectionHeader_t is implicitly C13 data, but we still have to register ourselves in any case

            /* We need to make sure that we register this memory only once. Having the CvDebugSSubsectionHeader type
             * be a class is one way to do that, but that allocates a bunch of memory. So Plan B: have the owner of the memory
             * check whether this chunk has been registered before. This also saves us from doing expensive lookups against
             * the global SymbolMemoryTracker table */

            var dataChunk = DataChunk;

            var codeViewAccessor = RegisterC13SymbolMemory(dataChunk);

            //Don't need to adjust the data + length to account for the header
            return new SymTypeList(dataChunk.Pointer, 0, Length, codeViewAccessor);
        }

        public CvDebugSLinesHeader GetLines()
        {
            if (Type != DEBUG_S_LINES && Type != DEBUG_S_IL_LINES)
                throw new InvalidOperationException($"Expected a section of type 'DEBUG_S_LINES' or 'DEBUG_S_IL_LINES' however the actual type was '{Type}'");

            return new CvDebugSLinesHeader(DataChunk, Length);
        }

        public RawValue<Utf8String>[] GetStringTable()
        {
            VerifyType(DEBUG_S_STRINGTABLE);

            var read = 0;

            var end = Length;

            using var results = new PooledList<RawValue<Utf8String>>();

            var dataChunk = DataChunk;

            while (read < end)
            {
                var str = dataChunk.PeekUtf8NullTerminatedString(read);
                results.Add(new RawValue<Utf8String>(dataChunk.AbsoluteOffset + read, str));
                read += str.Length + 1;
            }

            return results.ToArray();
        }

        //I tried to have a non-allocating collection type for this, but it's just too hard working with it,
        //because you need to be able to binary search and index into it all the time and everything. As such,
        //we'll have to settle for not storing the checksum array so we don't balloon our memory usage
        public CvFileCheckSum[] GetFileChecksums()
        {
            VerifyType(DEBUG_S_FILECHKSMS);

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

        public RvaAndFrameData GetFrameData()
        {
            VerifyType(DEBUG_S_FRAMEDATA);

            var data = new RvaAndFrameData(DataChunk, Length);

            return data;
        }

        public InlineeSigAndLines GetInlineeLines()
        {
            VerifyType(DEBUG_S_INLINEELINES);

            var dataChunk = DataChunk;

            var pdbFile = dataChunk.PDBFile();

            pdbFile.RegisterC13SymbolMemory(dataChunk);

            var sig = (CV_INLINEELINES_SIGNATURE) dataChunk.PeekUInt32(0);

            if (sig == CV_INLINEELINES_SIGNATURE.CV_INLINEE_SOURCE_LINE_SIGNATURE)
            {
                var length = Length - sizeof(int);

                var entries = new InlineeSourceLine[length / InlineeSourceLine.StructSize];

                for (var i = 0; i < entries.Length; i++)
                    entries[i] = new InlineeSourceLine(dataChunk.Slice(4 + (i * InlineeSourceLine.StructSize)));

                return new InlineeSigAndLines(dataChunk.AbsoluteOffset, sig, entries);
            }
            else
            {
                var read = sizeof(int);
                var length = Length;

                using var list = new PooledList<InlineeSourceLineEx>();

                while (read < length)
                {
                    var entry = new InlineeSourceLineEx(dataChunk.Slice(read));

                    read += entry.StructSize;

                    list.Add(entry);
                }

                Debug.Assert(read == length);

                return new InlineeSigAndLines(dataChunk.AbsoluteOffset, sig, list.ToArray());
            }
        }

        private CrossScopeReferences[] GetCrossScopeImports()
        {
            VerifyType(DEBUG_S_CROSSSCOPEIMPORTS);

            var length = Length;
            var read = 0;

            var dataChunk = DataChunk;

            using var list = new PooledList<CrossScopeReferences>();

            while (read < length)
            {
                var entry = new CrossScopeReferences(dataChunk.Slice(read));

                list.Add(entry);

                read += CrossScopeReferences.FixedStructSize + (entry.countOfCrossReferences * sizeof(int));
            }

            Debug.Assert(read == length);

            return list.ToArray();
        }

        public LocalIdAndGlobalIdPair[] GetCrossScopeExports()
        {
            VerifyType(DEBUG_S_CROSSSCOPEEXPORTS);

            var dataChunk = DataChunk;

            SymbolMemoryTracker.RegisterPDBSymbolMemory(dataChunk);

            var entries = new LocalIdAndGlobalIdPair[Length / LocalIdAndGlobalIdPair.StructSize];

            for (var i = 0; i < entries.Length; i++)
                entries[i] = new LocalIdAndGlobalIdPair(dataChunk.Slice(i * LocalIdAndGlobalIdPair.StructSize));

            return entries;
        }

        public FuncMDTokenMap GetFuncMDTokenMap()
        {
            VerifyType(DEBUG_S_FUNC_MDTOKEN_MAP);

            var parser = new FuncMDTokenMapParser();

            return parser.Parse(DataChunk, Length);
        }

        public TypeMDTokenMap GetTypeMDTokenMap()
        {
            VerifyType(DEBUG_S_TYPE_MDTOKEN_MAP);

            var parser = new TypeMDTokenMapParser();

            var dataChunk = DataChunk;

            //Need to register C13 symbol memory in order to resolve type indices
            RegisterC13SymbolMemory(dataChunk);

            return parser.Parse(DataChunk, Length);
        }

        public unsafe MergedAssemblyInfo[] GetMergedAssemblyInput()
        {
            VerifyType(DEBUG_S_MERGED_ASSEMBLYINPUT);

            /* SharedLibrary.pdb is the only PDB I've been able to find that has this.
             * SharedLibrary.dll seems to some kind of special ahead of time compiled DLL
             * included in the Windows SDK. It also has DEBUG_S_FUNC_MDTOKEN_MAP, DEBUG_S_TYPE_MDTOKEN_MAP
             * and DEBUG_S_IL_LINES, indicating that all four of these related to natively compiled
             * .NET code. I have seen DEBUG_S_IL_LINES in csc.ni.pdb as well
             * 
             * DumpModMergedAssemblyInput from cvdump.cpp shows how to parse this, however what's not
             * made obvious from DumpModMergedAssemblyInput is the fact that the "version" is in fact
             * a VS_VERSIONINFO
             */

            var dataChunk = DataChunk;

            var length = Length;

            var read = 0;

            using var results = new PooledList<MergedAssemblyInfo>();

            while (read < length)
            {
                //The StructSize takes care of the alignment
                var item = new MergedAssemblyInfo(dataChunk.Slice(read));
                results.Add(item);
                read += item.StructSize; //The StructSize handles 32-bit alignment
            }

            Debug.Assert(read == length);

            return results.ToArray();
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CV_DebugSSubsectionHeader_t, this, ViewKind.CvDebugSSubsectionHeader, StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        unsafe void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField("type", Type, sizeof(int));
            s.WriteField("cbLen", Length);

            switch (Type)
            {
                case DEBUG_S_SYMBOLS:
                    if (chunk.block is PagedMemoryBlock p)
                        s.WritePagedValue(chunk.RelativeOffset + 8, p, GetSymbols());
                    else
                    {
                        var symbols = GetSymbols();
                        structWriter.ViewWriter.ManualSymbolAccessor = symbols.codeViewAccessor ?? SymbolMemoryTracker.GetAccessor((long) (DataChunk.Pointer + 8));
                        s.WriteValue(Offset + 8, symbols);
                        structWriter.ViewWriter.ManualSymbolAccessor = null;
                    }
                    break;

                case DEBUG_S_LINES:
                case DEBUG_S_IL_LINES:
                    s.WriteInline(GetLines());
                    break;

                case DEBUG_S_STRINGTABLE:
                    s.WriteInlineUtf8NullTerminated(GetStringTable());
                    break;

                case DEBUG_S_FILECHKSMS:
                    s.WriteInline(GetFileChecksums());
                    break;

                case DEBUG_S_FRAMEDATA:
                    s.WriteInline(GetFrameData());
                    break;

                case DEBUG_S_INLINEELINES:
                    s.WriteInline(GetInlineeLines());
                    break;

                case DEBUG_S_CROSSSCOPEIMPORTS:
                    s.WriteInline(GetCrossScopeImports());
                    break;

                case DEBUG_S_CROSSSCOPEEXPORTS:
                    s.WriteInline(GetCrossScopeExports());
                    break;

                case DEBUG_S_FUNC_MDTOKEN_MAP:
                    s.WriteInline(GetFuncMDTokenMap());
                    break;

                case DEBUG_S_TYPE_MDTOKEN_MAP:
                    s.WriteInline(GetTypeMDTokenMap());
                    break;

                case DEBUG_S_MERGED_ASSEMBLYINPUT:
                    s.WriteInline(GetMergedAssemblyInput());
                    break;

                default:
                    Debug.Assert(false);

                    s.Pad(Length);
                    break;
            }

            structWriter.EagerFields = s.ToArray();
        }

        private void VerifyType(DEBUG_S_SUBSECTION_TYPE type)
        {
            if (Type != type)
                throw new InvalidOperationException($"Expected a section of type '{type}' however the actual type was '{Type}'");
        }

        private ICodeViewAccessor? RegisterC13SymbolMemory(in MemoryChunk dataChunk)
        {
            ICodeViewAccessor? codeViewAccessor;

            if (dataChunk.block is PagedMemoryBlock block)
            {
                block.PDBFile!.RegisterC13SymbolMemory(dataChunk);
                codeViewAccessor = block.PDBFile;
            }
            else
            {
                //OBJ or LIB. We're C13, which means UTF8

                if (dataChunk.block is GlobalMemoryBlock b)
                {
                    var objFile = (OBJFile) b.File;
                    codeViewAccessor = objFile.RegisterC13SymbolMemory(dataChunk);
                }
                else
                {
                    var s = (GlobalSubMemoryBlock) dataChunk.block;
                    var member = (LongImportLibraryMember) s.Owner;
                    codeViewAccessor = member.RegisterC13SymbolMemory(dataChunk);
                }
            }

            return codeViewAccessor;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
