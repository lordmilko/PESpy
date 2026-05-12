using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using PESpy.PDB;

namespace PESpy
{
    internal class SymbolReader : IDisposable
    {
        public SymbolValueList<VftableInfo> Vftables { get; }

        public SymbolValueList<Utf8String> NullTerminatedUtf8Strings { get; }
        public SymbolValueList<Utf16String> NullTerminatedUtf16Strings { get; }

        public SymbolValueList<FixedUtf8String> FixedUtf8Strings { get; }
        public SymbolValueList<FixedUtf16String> FixedUtf16Strings { get; }

        public SymbolValueList<RTTICompleteObjectLocator>? RTTICompleteObjectLocators { get; }

        //All symbols that are 32-bit __real@
        public SymbolValueList<float> Floats { get; }

        //All symbols that are 64-bit __real@
        public SymbolValueList<double> Doubles { get; }

        public NativeAOTModulesList? NativeAOTModules { get; }

        internal static bool TryCreate(PEFile peFile, ISymbolAccessor symbolAccessor, out SymbolReader symbolReader)
        {
            if (symbolAccessor is ExternalFileSymbolAccessor e && e.GetUnderlyingSymbolAccessorUnsafe() is PDBFileSymbolAccessor p)
            {
                var publicSymbols = p.PDBFile.PSGSI?.Symbols;

                //Managed PDBs don't have symbols
                if (publicSymbols != null)
                {
                    symbolReader = new SymbolReader(peFile, p.PDBFile, publicSymbols);
                    return true;
                }
            }

            symbolReader = default;
            return false;
        }

        private MemoryMappedFileHolder mmf;

        private unsafe SymbolReader(PEFile peFile, PDBFile pdbFile, GlobalSymTypeList symbols)
        {
            using var builder = new Builder(peFile);

            builder.InitializeFromPDB(pdbFile, symbols);

            var totalSymbols =
                builder._vftables.Count +
                builder._nullTerminatedUtf8Strings.Count +
                builder._nullTerminatedUtf16Strings.Count +
                builder._fixedUtf8Strings.Count +
                builder._fixedUtf16Strings.Count +
                builder._rttiCompleteObjectLocators.Count +
                builder._real8.Count +
                builder._real16.Count;

            mmf = new MemoryMappedFileHolder(totalSymbols * IntPtr.Size);

            var target = (SymType*) mmf.Address;

            Vftables                   = WriteSymbols(CreateVftable,                   builder._vftables.Span,                   ref target, peFile, pdbFile);
            NullTerminatedUtf8Strings  = WriteSymbols(CreateUtf8String,                builder._nullTerminatedUtf8Strings.Span,  ref target, peFile, pdbFile);
            NullTerminatedUtf16Strings = WriteSymbols(CreateUtf16String,               builder._nullTerminatedUtf16Strings.Span, ref target, peFile, pdbFile);
            FixedUtf8Strings           = WriteSymbols(CreateFixedUtf8String,           builder._fixedUtf8Strings.Span,           ref target, peFile, pdbFile);
            FixedUtf16Strings          = WriteSymbols(CreateFixedUtf16String,          builder._fixedUtf16Strings.Span,          ref target, peFile, pdbFile);
            RTTICompleteObjectLocators = WriteSymbols(CreateRttiCompleteObjectLocator, builder._rttiCompleteObjectLocators.Span, ref target, peFile, pdbFile);
            Floats                     = WriteSymbols(CreateReal8,                     builder._real8.Span,                      ref target, peFile, pdbFile);
            Doubles                    = WriteSymbols(CreateReal16,                    builder._real16.Span,                     ref target, peFile, pdbFile);

            NativeAOTModules = builder._nativeAOTModules;
        }

        private static unsafe SymbolValue<VftableInfo> CreateVftable(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            var peFile = (PEFile) file;

            //We already know all of these should succeed
            symType.TryGetRawOffSeg(out var off, out var seg);
            symType.TryGetLength(out var length, codeViewAccessor);
            peFile.TryGetOffset(off, seg - 1, out var offset);

            var name = symType.GetName(codeViewAccessor);

            var sectionIndex = seg - 1;
            ref var section = ref peFile.SectionHeaders[sectionIndex];

            var block = peFile.GetSectionBlock(sectionIndex, section);

            var pVftable = block.LocalPointer + off;

            NativeSpan<int> slots32 = default;
            NativeSpan<long> slots64 = default;

            if (peFile.Is32Bit)
                slots32 = new NativeSpan<int>(pVftable, length / sizeof(int));
            else
                slots64 = new NativeSpan<long>(pVftable, length / sizeof(long));

            //Our TryGetLength looks at the address map if available, which theoretically
            //should give us the same result (or better) as if we had collected all vftables and checked
            //their distance from each other at the end

            return new(
                block.RemoteStartOffset + off,
                symType,
                new VftableInfo(
                    (FixedUtf8String) name,
                    slots32,
                    slots64,
                    peFile.OptionalHeader.ImageBase,
                    (PDBFile) codeViewAccessor
                ),
                length
            );
        }

        private static unsafe SymbolValue<Utf8String> CreateUtf8String(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            GetStringInfo(file, codeViewAccessor, symType, out var offset, out var pString, out var strLength);

            return new(offset, symType, new Utf8String(pString), (int) strLength);
        }

        private static unsafe SymbolValue<Utf16String> CreateUtf16String(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            GetStringInfo(file, codeViewAccessor, symType, out var offset, out var pString, out var strLength);

            return new(offset, symType, new Utf16String((char*) pString), (int) strLength);
        }

        private static unsafe SymbolValue<FixedUtf8String> CreateFixedUtf8String(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            GetStringInfo(file, codeViewAccessor, symType, out var offset, out var pString, out var strLength);

            return new(offset, symType, new FixedUtf8String(pString, (int) strLength), (int) strLength);
        }

        private static unsafe SymbolValue<FixedUtf16String> CreateFixedUtf16String(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            GetStringInfo(file, codeViewAccessor, symType, out var offset, out var pString, out var strLength);

            return new(offset, symType, new FixedUtf16String((char*) pString, (int) strLength / 2), (int) strLength);
        }

        private static unsafe void GetStringInfo(
            IFile file,
            ICodeViewAccessor codeViewAccessor,
            SymType symType,
            out int offset,
            out byte* pString,
            out ulong strLength)
        {
            var peFile = (PEFile) file;

            var name = symType.GetName(codeViewAccessor);

            var textWindow = new Demangler.TextWindow(name.Value, name.Length, true);
            textWindow.AdvanceChar(6);

            textWindow.TryNextChar(out var stringKind);
            Demangler.TryParseNumber(ref textWindow, out _, out strLength);

            symType.TryGetRawOffSeg(out var off, out var seg);
            peFile.TryGetOffset(off, seg - 1, out offset);

            peFile.GetRawSectionDataFromRelativeOffset(off, seg - 1, out pString, out var _);
        }

        private static SymbolValue<RTTICompleteObjectLocator> CreateRttiCompleteObjectLocator(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            var peFile = (PEFile) file;

            symType.TryGetRawOffSeg(out var off, out var seg);

            peFile.TryGetValueChunkFromSection(off, seg - 1, out var chunk);

            //Note: apparently the first base class of a given type is always the main derived type
            var rttiCompleteObjectLocator = new RTTICompleteObjectLocator(chunk);

            return new(chunk.AbsoluteOffset, symType, rttiCompleteObjectLocator, RTTICompleteObjectLocator.StructSize);
        }

        private static unsafe SymbolValue<float> CreateReal8(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            symType.TryGetRawOffSeg(out var off, out var seg);
            ((PEFile) file).TryGetOffset(off, seg - 1, out var offset);

            var name = symType.GetName(codeViewAccessor);

            Utf8Parser.TryParse(name.AsSpan().Slice(7), out long i, out _, 'X');

            var f = *(float*) &i;

            return new(offset, symType, f, 4);
        }

        private static unsafe SymbolValue<double> CreateReal16(IFile file, ICodeViewAccessor codeViewAccessor, SymType symType)
        {
            symType.TryGetRawOffSeg(out var off, out var seg);
            ((PEFile) file).TryGetOffset(off, seg - 1, out var offset);

            var name = symType.GetName(codeViewAccessor);

            Utf8Parser.TryParse(name.AsSpan().Slice(7), out long i, out _, 'X');

            var d = *(double*) &i;

            return new(offset, symType, d, 8);
        }

        static unsafe SymbolValueList<T> WriteSymbols<T>(
            Func<IFile, ICodeViewAccessor, SymType, SymbolValue<T>> factory,
            Span<SymType> source,
            ref SymType* target,
            IFile file,
            ICodeViewAccessor codeViewAccessor)
        {
            if (source.Length == 0)
                return default;

            source.CopyTo(new Span<SymType>(target, source.Length));

            var symbols = new SymTypeCollection(target, source.Length);

            target += source.Length;

            return new SymbolValueList<T>(factory, symbols, file, codeViewAccessor);
        }

        public void Dispose()
        {
            mmf.Dispose();
        }

        public ref struct Builder
        {
            private PEFile _peFile;
            private long _imageBase;

            //As publics, the length of each vftable will be the length of their section contrib.
            //So we want to try and defer parsing these until we've read them all so we can clamp
            //the length of each item
            internal NativeList<SymType> _vftables;

            internal NativeList<SymType> _nullTerminatedUtf8Strings;
            internal NativeList<SymType> _nullTerminatedUtf16Strings;
            internal NativeList<SymType> _fixedUtf8Strings;
            internal NativeList<SymType> _fixedUtf16Strings;

            internal NativeList<SymType> _rttiCompleteObjectLocators;

            internal NativeList<SymType> _real8;
            internal NativeList<SymType> _real16;

            internal NativeAOTModulesList? _nativeAOTModules;

            internal Builder(PEFile peFile)
            {
                _peFile = peFile;
                _imageBase = _peFile.OptionalHeader.ImageBase;
            }

            internal unsafe void InitializeFromPDB(PDBFile pdbFile, GlobalSymTypeList symbols)
            {
                SymType nativeAOTModulesA = default;
                SymType nativeAOTModulesZ = default;

                foreach (var symType in symbols)
                {
                    if (!symType.TryGetName(out var name, pdbFile))
                        return;

                    var span = name.AsSpan();

                    /* We intentionally do not process the following symbols
                     * 
                     * __imp__               Sometimes these start with __imp_, other times they start with __imp__
                     *                       both of them seem to point to the same place, and can be intermingled;
                     *                       either way I feel like our IAT should already handle these
                     *
                     * __IMPORT_DESCRIPTOR   Any time you have an __IMPORT_DESCRIPTOR symbol, this points to an ImageImportDescriptor struct.
                     *                       I would expect that we already tagged all of these
                     */

                    //Keep in sync with FileAnalyzer.TryHandleSpecialPublic

                    if (span.StartsWith("??_C@_"u8))
                        ProcessString(symType, name.Value, span);
                    else if (span.StartsWith("??_7"u8))
                        ProcessVftable(symType, name, span, pdbFile);
                    else if (span.StartsWith("??_R4"u8)) //While each RTTI entity may have a symbol associated with it, we're only interested in matching the top level object locator type, which should point to all the rest
                        ProcessRTTI(symType);
                    else if (span.StartsWith("__real@"u8))
                        ProcessFloat(symType, name, span);
                    else if (span.StartsWith("__modules_"u8) && span.Length == 11)
                    {
                        switch (span[10])
                        {
                            case (byte) 'a':
                                nativeAOTModulesA = symType;
                                break;

                            case (byte) 'z':
                                nativeAOTModulesZ = symType;
                                break;
                        }
                    }
                }

                ProcessNativeAOTModules(_peFile, nativeAOTModulesA, nativeAOTModulesZ, out _nativeAOTModules);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ProcessString(
                SymType symType,
                byte* name,
                Span<byte> span)
            {
                //It's a string literal. If a 1 follows it's wide, if a 0 follows it's ANSI.
                //Then, following this is a length

                var textWindow = new Demangler.TextWindow(name, span.Length, true);
                textWindow.AdvanceChar(6);

                if (textWindow.TryNextChar(out var stringKind) && Demangler.TryParseNumber(ref textWindow, out _, out var strLength))
                {
                    if (!symType.TryGetRawOffSeg(out var off, out var seg) || !_peFile.TryGetOffset(off, seg - 1, out var offset))
                        return;

                    _peFile.GetRawSectionDataFromRelativeOffset(off, seg - 1, out var pString, out var _);

                    if (stringKind == '1')
                    {
                        if (*((ushort*) (pString + strLength - 2)) == 0)
                            _nullTerminatedUtf16Strings.Add(symType);
                        else
                            _fixedUtf16Strings.Add(symType);
                            
                    }
                    else
                    {
                        if (*(pString + strLength - 1) == 0)
                            _nullTerminatedUtf8Strings.Add(symType);
                        else
                            _fixedUtf8Strings.Add(symType);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ProcessVftable(SymType symType, SymString name, Span<byte> span, PDBFile pdbFile)
            {
                //It's a vftable. Add each entry as code in the work queue, and
                //also add xrefs from each slot to the target function

                if (!symType.TryGetRawOffSeg(out var off, out var seg) || !symType.TryGetLength(out var length, pdbFile) || !_peFile.TryGetOffset(off, seg - 1, out var offset))
                    return;

                _vftables.Add(symType);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ProcessRTTI(SymType symType)
            {
                /* MSVC defines the RTTI types in rttidata.h and uses them in rtti.cpp
                 * TypeDescriptor is defined in ehdata_forceinclude.h
                 * 
                 * The struct is typedef'd as the non-_s_ name. The comments above the struct
                 * use the non-_s_ name, so I think it's reasonable to call that its official name
                 * 
                 * Consider adding support for detecting CompleteObjLocator instances like ClassInformer does on the PEFile
                 * Need to support either using heuristics or using a PDB, which we then may or may not auto lookup or have the user provide,
                 * and if we auto look it up then we're responsible for disposing it
                 * https://github.com/kweatherman/IDA_ClassInformer_PlugIn/blob/master/RTTI.cpp
                 * 
                 * Note that in a shock twist, it doesn't actually seem that Visual Studio uses RTTI info itself;
                 * it just looks up the vftable and queries the type encoded in the name
                 */

                if (!symType.TryGetRawOffSeg(out var off, out var seg))
                    return;

                if (!_peFile.TryGetValueChunkFromSection(off, seg - 1, out var chunk))
                    return; //If this symbol doesn't point to valid memory, not much we can do

                _rttiCompleteObjectLocators.Add(symType);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ProcessFloat(SymType symType, SymString name, Span<byte> span)
            {
                //Ostensibly it's going to be __real@ followed by either 8 or 16 hex digits. I'm not sure if you could ever
                //have any other characters at the end; for now; we'll just assume it'll always be the simple case

                if (!symType.TryGetRawOffSeg(out var off, out var seg) || !_peFile.TryGetOffset(off, seg - 1, out var offset))
                    return;

                switch (span.Length)
                {
                    case 8 + 7: //__real@ + 8 chars
                        if (!Utf8Parser.TryParse(span.Slice(7), out long i, out _, 'X'))
                            return;

                        var f = *(float*) &i;

                        _real8.Add(symType);
                        break;

                    case 16 + 7: //__real@ + 16 chars
                        if (!Utf8Parser.TryParse(span.Slice(7), out long l, out _, 'X'))
                            return;

                        var d = *(double*) &l;

                        _real16.Add(symType);
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle symbol '{name}'");
                }
            }

            internal static void ProcessNativeAOTModules(
                PEFile peFile,
                SymType nativeAOTModulesA,
                SymType nativeAOTModulesZ,
                out NativeAOTModulesList nativeAOTModules)
            {
                nativeAOTModules = default;

                if (nativeAOTModulesA != default && nativeAOTModulesZ != default)
                {
                    //I'm expecting this data should span only a single section, so we'll just check that the sections are the same so we don't
                    //need to spend time doing RVA math
                    if (nativeAOTModulesA.TryGetRawOffSeg(out var offA, out var segA) && nativeAOTModulesZ.TryGetRawOffSeg(out var offZ, out var segZ) && segA == segZ)
                    {
                        if (peFile.TryGetValueChunkFromSection(offA, segA - 1, out var chunk))
                        {
                            //On the one hand, we want to ensure that __modules_z is included
                            //in this. However, when NativeOAT calculates the number of modules
                            //present it does not include the pointer at __modules_z in the count,
                            //so we shouldn't either
                            var numModules = ((offZ - offA) / chunk.PointerSize);

                            nativeAOTModules = new NativeAOTModulesList(chunk, numModules);
                        }
                    }
                }
            }

            public void Dispose()
            {
                _nullTerminatedUtf8Strings.Dispose();
                _nullTerminatedUtf16Strings.Dispose();
                _fixedUtf8Strings.Dispose();

                _rttiCompleteObjectLocators.Dispose();

                _real8.Dispose();
                _real16.Dispose();
            }
        }
    }
}
