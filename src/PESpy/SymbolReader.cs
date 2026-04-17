using System;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using PESpy.PDB;

namespace PESpy
{
    internal class SymbolReader
    {
        public VftableInfo[]? Vftables { get; }

        public SymbolValue<Utf8String>[] NullTerminatedUtf8Strings { get; }
        public SymbolValue<Utf16String>[] NullTerminatedUtf16Strings { get; }

        public SymbolValue<FixedUtf8String>[] FixedUtf8Strings { get; }
        public SymbolValue<FixedUtf16String>[] FixedUtf16Strings { get; }

        public RTTICompleteObjectLocator[]? RTTICompleteObjectLocators { get; }

        //All symbols that are 32-bit __real@
        public SymbolValue<float>[] Floats { get; }

        //All symbols that are 64-bit __real@
        public SymbolValue<double>[] Doubles { get; }

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

        private unsafe SymbolReader(PEFile peFile, PDBFile pdbFile, GlobalSymTypeList symbols)
        {
            using var builder = new Builder(peFile);

            builder.InitializeFromPDB(pdbFile, symbols);

            if (builder._vftableInfos.Count > 0)
                Vftables = builder._vftableInfos.ToArray();

            NullTerminatedUtf8Strings = builder._nullTerminatedUtf8Strings.ToArray();
            NullTerminatedUtf16Strings = builder._nullTerminatedUtf16Strings.ToArray();

            FixedUtf8Strings = builder._fixedUtf8Strings.ToArray();
            FixedUtf16Strings = builder._fixedUtf16Strings.ToArray();

            if (builder._rttiCompleteObjectLocators.Count > 0)
                RTTICompleteObjectLocators = builder._rttiCompleteObjectLocators.ToArray();

            Floats = builder._real8.ToArray();
            Doubles = builder._real16.ToArray();

            NativeAOTModules = builder._nativeAOTModules;
        }

        public ref struct Builder
        {
            private PEFile _peFile;
            private long _imageBase;

            //As publics, the length of each vftable will be the length of their section contrib.
            //So we want to try and defer parsing these until we've read them all so we can clamp
            //the length of each item
            internal PooledList<VftableInfo> _vftableInfos;

            internal PooledList<SymbolValue<Utf8String>> _nullTerminatedUtf8Strings;
            internal PooledList<SymbolValue<Utf16String>> _nullTerminatedUtf16Strings;
            internal PooledList<SymbolValue<FixedUtf8String>> _fixedUtf8Strings;
            internal PooledList<SymbolValue<FixedUtf16String>> _fixedUtf16Strings;

            internal PooledList<RTTICompleteObjectLocator> _rttiCompleteObjectLocators;

            internal PooledList<SymbolValue<float>> _real8;
            internal PooledList<SymbolValue<double>> _real16;

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

                    if (span.StartsWith("??_C@_"u8))
                        ProcessString(symType, name, span);
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

                ProcessNativeAOTModules(nativeAOTModulesA, nativeAOTModulesZ);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ProcessString(
                SymType symType,
                SymString name,
                Span<byte> span)
            {
                //It's a string literal. If a 1 follows it's wide, if a 0 follows it's ANSI.
                //Then, following this is a length

                var textWindow = new Demangler.TextWindow(name.Value, span.Length, true);
                textWindow.AdvanceChar(6);

                if (textWindow.TryNextChar(out var stringKind) && Demangler.TryParseNumber(ref textWindow, out _, out var strLength))
                {
                    if (!symType.TryGetOffSeg(out var off, out var seg) || !_peFile.TryGetOffset(off, seg - 1, out var offset))
                        return;

                    _peFile.GetRawSectionDataFromRelativeOffset(off, seg - 1, out var pString, out var _);

                    if (stringKind == '1')
                    {
                        if (*((ushort*) (pString + strLength - 2)) == 0)
                            _nullTerminatedUtf16Strings.Add(new(offset, symType, new Utf16String((char*) pString), (int) strLength));
                        else
                            _fixedUtf16Strings.Add(new(offset, symType, new FixedUtf16String((char*) pString, (int) strLength / 2), (int) strLength));
                    }
                    else
                    {
                        if (*(pString + strLength - 1) == 0)
                            _nullTerminatedUtf8Strings.Add(new(offset, symType, new Utf8String(pString), (int) strLength));
                        else
                            _fixedUtf8Strings.Add(new(offset, symType, new FixedUtf8String(pString, (int) strLength), (int) strLength));
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ProcessVftable(SymType symType, SymString name, Span<byte> span, PDBFile pdbFile)
            {
                //It's a vftable. Add each entry as code in the work queue, and
                //also add xrefs from each slot to the target function

                if (!symType.TryGetOffSeg(out var off, out var seg) || !symType.TryGetLength(out var length, pdbFile) || !_peFile.TryGetOffset(off, seg - 1, out var offset))
                    return;

                _peFile.GetRawSectionDataFromRelativeOffset(off, seg - 1, out var pVftable, out var remainingLength);

                if (length > remainingLength)
                    return; //We clearly don't know what the real length is

                NativeSpan<int> slots32 = default;
                NativeSpan<long> slots64 = default;

                if (_peFile.Is32Bit)
                    slots32 = new NativeSpan<int>(pVftable, length / sizeof(int));
                else
                    slots64 = new NativeSpan<long>(pVftable, length / sizeof(long));

                //Our TryGetLength looks at the address map if available, which theoretically
                //should give us the same result (or better) as if we had collected all vftables and checked
                //their distance from each other at the end

                _vftableInfos.Add(
                    new VftableInfo(
                        offset,
                        symType,
                        new FixedUtf8String(name.Value, span.Length), //Avoid re-measuring the length of the string
                        slots32,
                        slots64,
                        _imageBase,
                        pdbFile,
                        length
                    )
                );
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

                if (!symType.TryGetOffSeg(out var off, out var seg))
                    return;

                if (!_peFile.TryGetValueChunkFromSection(off, seg - 1, out var chunk))
                    return; //If this symbol doesn't point to valid memory, not much we can do

                //Note: apparently the first base class of a given type is always the main derived type
                var rttiCompleteObjectLocator = new RTTICompleteObjectLocator(chunk);

                _rttiCompleteObjectLocators.Add(rttiCompleteObjectLocator);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private unsafe void ProcessFloat(SymType symType, SymString name, Span<byte> span)
            {
                //Ostensibly it's going to be __real@ followed by either 8 or 16 hex digits. I'm not sure if you could ever
                //have any other characters at the end; for now; we'll just assume it'll always be the simple case

                if (!symType.TryGetOffSeg(out var off, out var seg) || !_peFile.TryGetOffset(off, seg - 1, out var offset))
                    return;

                switch (span.Length)
                {
                    case 8 + 7: //__real@ + 8 chars
                        if (!Utf8Parser.TryParse(span.Slice(7), out long i, out _, 'X'))
                            return;

                        var f = *(float*) &i;

                        _real8.Add(new(offset, symType, f, 4));
                        break;

                    case 16 + 7: //__real@ + 16 chars
                        if (!Utf8Parser.TryParse(span.Slice(7), out long l, out _, 'X'))
                            return;

                        var d = *(double*) &l;

                        _real16.Add(new(offset, symType, d, 8));
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle symbol '{name}'");
                }
            }

            private void ProcessNativeAOTModules(SymType nativeAOTModulesA, SymType nativeAOTModulesZ)
            {
                if (nativeAOTModulesA != default && nativeAOTModulesZ != default)
                {
                    //I'm expecting this data should span only a single section, so we'll just check that the sections are the same so we don't
                    //need to spend time doing RVA math
                    if (nativeAOTModulesA.TryGetOffSeg(out var offA, out var segA) && nativeAOTModulesZ.TryGetOffSeg(out var offZ, out var segZ) && segA == segZ)
                    {
                        if (_peFile.TryGetValueChunkFromSection(offA, segA - 1, out var chunk))
                        {
                            //On the one hand, we want to ensure that __modules_z is included
                            //in this. However, when NativeOAT calculates the number of modules
                            //present it does not include the pointer at __modules_z in the count,
                            //so we shouldn't either
                            var numModules = ((offZ - offA) / chunk.PointerSize);

                            _nativeAOTModules = new NativeAOTModulesList(chunk, numModules);
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
