using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy.View
{
    internal unsafe class PEViewByteViewWriter : PEViewWriter
    {
        private readonly FileAccessor _fileAccessor;
        private readonly IFileDisassembler? _fileDisassembler;
        private readonly FileAnalyzer _fileAnalyzer;

        private bool _inRegion;
        private RegionBuilder _currentRegion;
        private Stack<RegionBuilder> _priorRegionStack = new Stack<RegionBuilder>();
        internal List<(int start, int end, IFile file)> _nestedFileRanges = new List<(int start, int end, IFile file)>();
        private int _nestedFileDepth;

        internal List<RegionBuilder> _topLevelRegions = new List<RegionBuilder>();
        internal List<RegionBuilder> _firstRegionByAddress = new List<RegionBuilder>();

        public PEViewByteViewWriter(
            PEFile peFile,
            ViewMode mode,
            FileAccessor fileAccessor,
            IFileDisassembler fileDisassembler,
            FileAnalyzer fileAnalyzer,
            LocatorHttpPolicy httpPolicy,
            ILocatorProgress progress) : base(peFile, peFile.CreateByteViewProvider(null), mode)
        {
            _fileAccessor = fileAccessor;
            _fileDisassembler = fileDisassembler;
            _fileAnalyzer = fileAnalyzer;
            _httpPolicy = httpPolicy;
            _progress = progress;
        }

        protected internal override IView? NewUnmanagedStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            throw new System.NotImplementedException();
        }

        protected internal override unsafe IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            //Don't use FileAccessor.AddStruct here because we need to special case the body of IL methods

            TryGetViewOffset(value.Offset, out var offset);

            if (_inRegion && FromRegion)
            {
                Debug.Assert(_currentRegion.End == offset);
                _currentRegion.End += structSize;
            }

            //Every struct will call NewStruct(), so we want to take steps to minimize its size in NativeAOT

            var pViewByte = RegisterStruct(name, offset, kind);

            for (var i = pViewByte + 1; i < pViewByte + structSize; i++)
                i->Kind = ViewByteKind.Body;
            return null;
        }

        internal override RegionWriter CreateRegion(int offset, string name, ViewKind kind, bool global = false, ViewWriter nestedViewWriter = null)
        {
            EnterRegion(offset, name, global, kind);

            return base.CreateRegion(offset, name, kind, global, nestedViewWriter);
        }

        internal override RegionWriter CreateRegion(int offset, int structOffset, int fieldOffset, string name, ViewKind kind, bool global
#if DEBUG
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
            , long listedAddress
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
            , ViewWriter nestedViewWriter
            )
        {
            EnterRegion(offset, name, global, kind);

            return base.CreateRegion(
                offset,
                structOffset,
                fieldOffset,
                name,
                kind,
                global
#if DEBUG
                , listedAddress
#endif
                , nestedViewWriter
            );
        }

        internal override RegionWriter CreateScopedRegion(int offset, int structOffset, int fieldOffset, string name, ViewKind kind, ViewKind scopeKind
#if DEBUG
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
            , int listedOffset
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#endif
            , ViewWriter nestedViewWriter
            )
        {
            EnterRegion(offset, name, global: false, kind);

            return base.CreateScopedRegion(
                offset,
                structOffset,
                fieldOffset,
                name,
                kind,
                scopeKind
#if DEBUG
                , listedOffset
#endif
                , nestedViewWriter
            );
        }

        private void EnterRegion(int offset, string name, bool global, ViewKind kind)
        {
            var shouldAdd = tryGetViewOffset(offset, out offset);

            var newRegion = new RegionBuilder
            {
                Name = name,
                Start = offset,
                End = offset,
                Kind = kind,
                IsGlobal = global,
                Depth = _inRegion ? _priorRegionStack.Count + 1 : 0,
                NestedFileDepth = _nestedFileDepth
            };

            if (_inRegion)
            {
                if (_currentRegion.NestedFileDepth == _nestedFileDepth)
                {
                    if (_currentRegion.Children == null)
                        _currentRegion.Children = new List<RegionBuilder>();

                    _currentRegion.Children.Add(newRegion);
                }

                _priorRegionStack.Push(_currentRegion);

                if (newRegion.Start != _currentRegion.Start)
                    _firstRegionByAddress.Add(newRegion);
            }
            else
                _firstRegionByAddress.Add(newRegion);

            _currentRegion = newRegion;
            _inRegion = true;
        }

        internal override void ExitRegion()
        {
            var previousRegion = _currentRegion;
            Debug.Assert(previousRegion.Length > 0);

            if (_currentRegion.Depth == 0)
                _topLevelRegions.Add(_currentRegion);

            if (_priorRegionStack.Count > 0)
            {
                _currentRegion = _priorRegionStack.Pop();

                if (_currentRegion.NestedFileDepth == previousRegion.NestedFileDepth - 1)
                {
                    Debug.Assert(_currentRegion.Depth != previousRegion.Length);

                    //This is conceptually top level too
                    _topLevelRegions.Add(previousRegion);
                }
                else
                {
                    if (!previousRegion.IsGlobal && _currentRegion.NestedFileDepth == previousRegion.NestedFileDepth)
                        _currentRegion.End += previousRegion.Length;
                }
            }
            else
            {
                _currentRegion = default;
                _inRegion = false;
            }
        }

        internal override void EnterNestedFile(int startOffset, int length, IFile file)
        {
            _nestedFileRanges.Add((startOffset, startOffset + length, file));

            _nestedFileDepth++;
        }

        internal override void ExitNestedFile()
        {
            _nestedFileDepth--;
        }

        public override void WriteOffsetXRef(int structOffset, int fieldOffset, int targetOffset)
        {
            if (targetOffset == 0)
                return;

            //The offsets we're given needs to be converted to ViewMode space. We can't trust
            //the target, as that may not exist in the current ViewMode
            TryGetViewOffset(structOffset, out structOffset);

            if (_fileAccessor.TryGetTargetAddress(targetOffset, out var targetAddress, out _))
                _fileAnalyzer.AddXRef(structOffset + fieldOffset, targetAddress);
        }

        public override void WriteRVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            if (targetRVA == 0)
                return;

            if (_fileAccessor.TryGetTargetAddress(targetRVA, out var targetAddress, out _))
                _fileAnalyzer.AddXRef(structOffset + fieldOffset, targetAddress);
        }

        public override void WriteVAXRef(int structOffset, int fieldOffset, int targetVA)
        {
            throw new NotImplementedException();
        }

        private ViewByte* RegisterStruct(FixedUtf8String name, int offset, ViewKind kind)
        {
            var pViewByte = _fileAccessor.GetViewByte(offset, out _);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;
            _fileAnalyzer.AddName(offset, pViewByte, name);
            _fileAccessor.AddStructKind(offset, kind);

            return pViewByte;
        }

        private void ProcessCorILMethodTiny(ViewByte* pViewByte, ImageCorILMethod value)
        {
            //The first byte is data, but all of the bytes after it are code

            var ilBytes = value.ILBytes;

            var pILViewByte = pViewByte + 1;
            pILViewByte->Kind = ViewByteKind.Code;
            pILViewByte->IsIL = true;

            //The name of the method is computed in PEFileAnalyzer.ProcessEcmaMetadata

            for (var i = pILViewByte + 1; i < pILViewByte + ilBytes.Length; i++)
                i->Kind = ViewByteKind.Body;
        }

        protected internal override unsafe IView? NewValue<T>(int offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            //Note that if we're pretending to be virtual when we're physical, we _don't_ need to update
            //the offset here, because the caller should have done that for us and the offset we receive
            //here should already be in virtual space. This is true when writing globals or writing items
            //inside a region

            return RegisterValue(offset, size, kind, fromRegion);
        }

        public override void WriteGlobalField<T>(int offset, FixedUtf8String name, in T value, int size, ViewKind kind)
        {
            throw new NotImplementedException();
        }

        private IView? RegisterValue(
            int offset,
            int size,
            ViewKind kind,
            bool fromRegion)
        {
            if (_inRegion && fromRegion)
            {
                Debug.Assert(_currentRegion.End == offset);
                _currentRegion.End += size;
            }

            var pViewByte = RegisterValueInternal(_fileAccessor, _fileAnalyzer, offset, size, kind, out _);

            for (var i = pViewByte + 1; i < pViewByte + size; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }

        internal static ViewByte* RegisterValueInternal(
            FileAccessor fileAccessor,
            FileAnalyzer fileAnalyzer,
            int offset,
            int size,
            ViewKind kind,
            out int sectionAccessorIndex)
        {
            fileAccessor.AddStructKind(offset, kind);

            var pViewByte = fileAccessor.GetViewByte(offset, out sectionAccessorIndex);
            pViewByte->Kind = ViewByteKind.Data;

            switch (kind)
            {
                case ViewKind.ImageExportDirectory_Name:
                case ViewKind.ImageExportDirectory_AddressOfNames_Name:
                case ViewKind.Metadata_String:
                case ViewKind.ImageImportDescriptor_Name:
                case ViewKind.ImageEnclaveImport_ImportName:
                case ViewKind.Manifest:
                case ViewKind.ImageDelayLoadDescriptor_DllNameRVA:
                case ViewKind.ImageExportDirectory_ForwarderName:
                case ViewKind.ImageBoundImportName:
                case ViewKind.DepsJson:
                case ViewKind.RuntimeConfigJson:
                    pViewByte->DataKind = ViewByteDataKind.String;
                    break;

                case ViewKind.Metadata_Guid:
                    pViewByte->DataKind = ViewByteDataKind.Guid;
                    break;

                case ViewKind.ImageExportDirectory_AddressOfNames_Entry:
                case ViewKind.ImageExportDirectory_AddressOfFunctions_Entry:
                case ViewKind.ImageExportDirectory_AddressOfNameOrdinals_Entry:
                case ViewKind.SecurityCookie:
                case ViewKind.GuardCFCheckFunctionPointer:
                case ViewKind.GuardCFDispatchFunctionPointer:
                case ViewKind.XFG:
                case ViewKind.GuardXFGCheckFunctionPointer:
                case ViewKind.GuardXFGDispatchFunctionPointer:
                case ViewKind.GuardXFGTableDispatchFunctionPointer:
                case ViewKind.GuardMemcpyFunctionPointer:
                case ViewKind.GuardRFFailureRoutineFunctionPointer:
                case ViewKind.GuardRFVerifyStackPointerFunctionPointer:
                case ViewKind.CastGuardOsDeterminedFailureMode:
                case ViewKind.ImageDelayLoadDescriptor_ModuleHandleRVA:
                case ViewKind.PN:
                case ViewKind.LockPrefixTable: //Array
                case ViewKind.NativeAOTModulesA:
                case ViewKind.NativeAOTModuleAddress:
                case ViewKind.NativeAOTModulesZ:
                    pViewByte->DataKind = ViewByteDataKind.Integer;
                    break;

                case ViewKind.CvSignature:
                case ViewKind.ExDllCharacteristics:
                case ViewKind.PdbFeature:
                    pViewByte->DataKind = ViewByteDataKind.Enum;
                    break;

                case ViewKind.SEHandlerTable:
                case ViewKind.HRFile:
                case ViewKind.HashBuckets:
                case ViewKind.HashBucketsBitmap:
                    pViewByte->DataKind = ViewByteDataKind.Struct;
                    break;

                default:
                    throw new System.NotImplementedException();
            }

            /* Certain values have well known symbol names. We don't want to apply these names if
             * it turns out we'll have symbols however, as that'll cause us to double up. So instead,
             * we'll collect a series of "pending" names, and then figure out whether or not we want
             * to commit them after we know what kind of symbols we have
             */

            switch (kind)
            {
                case ViewKind.GuardAddressTakenIatEntryTable:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_iat_table);
                    break;

                case ViewKind.GuardCFFunctionTable:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_fids_table);
                    break;

                case ViewKind.GuardEHContinuationTable:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_eh_cont_table);
                    break;

                case ViewKind.GuardLongJumpTargetTable:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_longjmp_table);
                    break;

                case ViewKind.SecurityCookie:
                    fileAnalyzer.AddPendingName(offset, Strings.__security_cookie);
                    break;

                case ViewKind.SEHandlerTable:
                    fileAnalyzer.AddPendingName(offset, Strings.__safe_se_handler_table);
                    break;

                case ViewKind.GuardCFCheckFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_check_icall_fptr);
                    break;

                case ViewKind.GuardCFDispatchFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_dispatch_icall_fptr);
                    break;

                case ViewKind.GuardRFFailureRoutineFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_ss_verify_failure_fptr);
                    break;

                case ViewKind.GuardRFVerifyStackPointerFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_ss_verify_sp_fptr);
                    break;

                case ViewKind.GuardXFGCheckFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_xfg_check_icall_fptr);
                    break;

                case ViewKind.GuardXFGDispatchFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_xfg_dispatch_icall_fptr);
                    break;

                case ViewKind.GuardXFGTableDispatchFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_xfg_table_dispatch_icall_fptr);
                    break;

                case ViewKind.CastGuardOsDeterminedFailureMode:
                    fileAnalyzer.AddPendingName(offset, Strings.__castguard_check_failure_os_handled_fptr);
                    break;

                case ViewKind.GuardMemcpyFunctionPointer:
                    fileAnalyzer.AddPendingName(offset, Strings.__guard_memcpy_fptr);
                    break;
            }

            return pViewByte;
        }

        public override void WriteDosStub(in ByteBlob byteBlob)
        {
            if (_fileDisassembler != null)
                _fileDisassembler.WriteDosStub(_fileAccessor, _fileAnalyzer, byteBlob);
            else
            {
                if (byteViewProvider.TryParseRawBytes(
                    byteBlob.Offset,
                    ViewKind.DosStub,
                    byteBlob.Bytes,
                    null,
                    out var views))
                {
                    var fileAccessor = _fileAccessor;

                    foreach (var view in views)
                    {
                        var pViewByte = fileAccessor.GetViewByte(view.Offset, out var sectionAccessorIndex);
                        pViewByte->Kind = ViewByteKind.Data;

                        for (var i = pViewByte + 1; i < pViewByte + view.Size; i++)
                            i->Kind = ViewByteKind.Body;

                        switch (view.Kind)
                        {
                            case ViewKind.DosStub:
                                _fileAccessor.AddStructKind(view.Offset, view.Kind);
                                pViewByte->Kind = ViewByteKind.Code; //That leading byte should be code then, not data. Not sure if it's a bad idea to have code with a ViewKind on it?
                                break;

                            case ViewKind.String:
                                pViewByte->DataKind = ViewByteDataKind.String;
                                break;

                            case ViewKind.Padding:
                                pViewByte->DataKind = ViewByteDataKind.Padding;
                                break;
                        }
                    }
                }
            }
        }

        public override void WriteIL(int offset, NativeSpan<byte> ilBytes)
        {
            TryGetViewOffset(offset, out offset);

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);

            pViewByte->Kind = ViewByteKind.Code;
            pViewByte->IsIL = true;

            for (var i = pViewByte + 1; i < pViewByte + ilBytes.Length; i++)
                i->Kind = ViewByteKind.Body;
        }

        public override ByteBlobView? WriteByteBlob(ByteBlob byteBlob)
        {
            TryGetViewOffset(byteBlob.Offset, out var offset);

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Integer; //Bytes
            _fileAccessor.AddStructKind(offset, byteBlob.viewKind);

            for (var i = pViewByte + 1; i < pViewByte + byteBlob.Bytes.Length; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }

        public override ByteBlobView? WritePadding(int offset, NativeSpan<byte> bytes)
        {
            TryGetViewOffset(offset, out offset);

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);
            Debug.Assert(pViewByte->Kind != ViewByteKind.Body);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Padding;

            for (var i = pViewByte + 1; i < pViewByte + bytes.Length; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }
    }
}
