using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy.View
{
    internal unsafe class ViewByteViewWriter : ViewWriter
    {
        private readonly IFileDisassembler? _fileDisassembler;
        private readonly FileAnalyzer _fileAnalyzer;

        private bool _inRegion;
        private RegionBuilder _currentRegion;
        private Stack<RegionBuilder> _priorRegionStack = new Stack<RegionBuilder>();
        internal List<(int start, int end, PEFile file)> _nestedFileRanges = new List<(int start, int end, PEFile file)>();
        private int _nestedFileDepth;

        internal UnwindInfoHashSet _unwindInfos;

        internal List<RegionBuilder> _topLevelRegions = new List<RegionBuilder>();
        internal List<RegionBuilder> _firstRegionByAddress = new List<RegionBuilder>();

        //Regardless of whether it's __GSHandlerCheck_EH4 or __CxxFrameHandler4, either way
        //the ExceptionData will start with an RVA<FuncInfo4>. We also add FuncInfo and SCOPE_TABLE
        //entries to this so we can collect code from them later
        internal Dictionary<int, UnwindInfo.SpecialExceptionDataKind> _specialUnwindInfos = new();

        public ViewByteViewWriter(
            IViewWriterHelper helper,
            ByteViewProvider byteViewProvider,
            ViewMode mode,
            FileAccessor fileAccessor,
            IFileDisassembler fileDisassembler,
            FileAnalyzer fileAnalyzer,
            LocatorHttpPolicy httpPolicy,
            ILocatorProgress progress) : base(helper, byteViewProvider, mode, fileAccessor)
        {
            _fileDisassembler = fileDisassembler;
            _fileAnalyzer = fileAnalyzer;
            _httpPolicy = httpPolicy;
            _progress = progress;

            IsByteViewWriter = true;
        }

        protected internal override IView? NewUnmanagedStruct<T>(in T value, ViewKind kind, int structSize)
        {
            //ViewWriter translates UnmanagedOffset to physical/virtual space for us, so we don't need to worry
            //about adjusting it
            NewStruct(UnmanagedOffset, structSize, kind);
            return null;
        }

        protected internal override unsafe IView? NewStruct<T>(in T value, ViewKind kind, int structSize)
        {
            NewStruct(value.Offset, structSize, kind);
            return null;
        }

        internal void NewStruct(long offset, int structSize, ViewKind kind)
        {
            //Don't use FileAccessor.AddStruct here because we need to special case the body of IL methods

            if (!TryGetViewOffset(offset, out offset))
                return;

            if (_inRegion && FromRegion)
            {
                Debug.Assert(_currentRegion.End == offset);
                _currentRegion.End += structSize;
            }

            //Every struct will call NewStruct(), so we want to take steps to minimize its size in NativeAOT

            var pViewByte = _fileAccessor.GetViewByte(offset, out _);
            RegisterStruct(pViewByte, offset, kind);

            for (var i = pViewByte + 1; i < pViewByte + structSize; i++)
                i->Kind = ViewByteKind.Body;
        }

        internal override RegionWriter CreateRegion(long offset, string name, ViewKind kind, bool global = false, ViewWriter nestedViewWriter = null)
        {
            EnterRegion(offset, name, global, kind);

            return base.CreateRegion(offset, name, kind, global, nestedViewWriter);
        }

        internal override RegionWriter CreateRegion(long offset, long structOffset, int fieldOffset, string name, ViewKind kind, bool global
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

        internal override RegionWriter CreateScopedRegion(long offset, long structOffset, int fieldOffset, string name, ViewKind kind, ViewKind scopeKind
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

        private void EnterRegion(long offset, string name, bool global, ViewKind kind)
        {
            var shouldAdd = TryGetViewOffset(offset, out offset);

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

        internal override void EnterNestedFile(int startOffset, int length, PEFile file)
        {
            _nestedFileRanges.Add((startOffset, startOffset + length, file));

            _nestedFileDepth++;
        }

        internal override void ExitNestedFile()
        {
            _nestedFileDepth--;
        }

        public override void WriteOffsetXRef(long structOffset, int fieldOffset, long targetOffset)
        {
#if DEBUG
            VerifyWritingUniqueXRef();
#endif

            if (targetOffset == 0)
                return;

            //The offsets we're given needs to be converted to ViewMode space. We can't trust
            //the target, as that may not exist in the current ViewMode
            if (!TryGetViewOffset(structOffset, out structOffset))
                return;

            //Don't use TryGetTargetAddress because targetOffset is not an RVA
            if (TryGetViewOffset(targetOffset, out var targetAddress))
                _fileAnalyzer.AddXRef((int) structOffset + fieldOffset, (int) targetAddress);
        }

        public override void WriteRVAXRef(long structOffset, int fieldOffset, int targetRVA)
        {
#if DEBUG
            VerifyWritingUniqueXRef();
#endif

            if (targetRVA == 0)
                return;

            if (!TryGetViewOffset(structOffset, out structOffset))
                return;

            if (_fileAccessor.TryGetTargetAddress(targetRVA, out var targetAddress, out _))
                _fileAnalyzer.AddXRef((int) structOffset + fieldOffset, targetAddress);
        }

        public void WriteTargetAddressXRef(long structOffset, int fieldOffset, int targetRVA)
        {
#if DEBUG
            VerifyWritingUniqueXRef();
#endif

            if (targetRVA == 0)
                return;

            //structOffset is already in targetAddress space, so we don't need to convert it

            if (_fileAccessor.TryGetTargetAddress(targetRVA, out var targetAddress, out _))
                _fileAnalyzer.AddXRef((int) structOffset + fieldOffset, targetAddress);
        }

        public override void WriteVAXRef(long structOffset, int fieldOffset, long targetVA)
        {
#if DEBUG
            VerifyWritingUniqueXRef();
#endif

            if (targetVA == 0)
                return;

            var rva = (int) (targetVA - _fileAccessor.ImageBase);

            //structOffset is already in targetAddress space, so we don't need to convert it

            if (_fileAccessor.TryGetTargetAddress(rva, out var targetAddress, out _))
                _fileAnalyzer.AddXRef((int) structOffset + fieldOffset, targetAddress);
        }

        internal void RegisterStruct(ViewByte* pViewByte, long offset, ViewKind kind)
        {
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Struct;
            _fileAccessor.CheckName(offset);
            pViewByte->HasName = true;
            _fileAccessor.AddStructKind(offset, kind);
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

        protected internal override unsafe IView? NewValue<T>(long offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            //Note that if we're pretending to be virtual when we're physical, we _don't_ need to update
            //the offset here, because the caller should have done that for us and the offset we receive
            //here should already be in virtual space. This is true when writing globals or writing items
            //inside a region

            return RegisterValue(offset, size, kind, fromRegion);
        }

        public override void WriteGlobalField<T>(long offset, in T value, int size, ViewKind kind) =>
            RegisterGlobalField(offset, size, kind);

        private void RegisterGlobalField(long offset, int size, ViewKind kind)
        {
            if (!TryGetViewOffset(offset, out offset))
                return;

            //While it's not really a struct, we treat it like one since it has a ViewKind and then special
            //case it accordingly
            var pViewByte = _fileAccessor.GetViewByte(offset, out _);
            RegisterStruct(pViewByte, offset, kind);

            for (var i = pViewByte + 1; i < pViewByte + size; i++)
                i->Kind = ViewByteKind.Body;
        }

        private IView? RegisterValue(
            long offset,
            int size,
            ViewKind kind,
            bool fromRegion)
        {
            if (_inRegion && fromRegion)
            {
                Debug.Assert(_currentRegion.End == offset);
                _currentRegion.End += size;
            }

            var pViewByte = RegisterValueInternal(offset, size, kind, out _);

            for (var i = pViewByte + 1; i < pViewByte + size; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }

        protected ViewByte* RegisterValueInternal(
            long offset,
            int size,
            ViewKind kind,
            out int sectionAccessorIndex)
        {
            _fileAccessor.AddStructKind(offset, kind);

            var pViewByte = _fileAccessor.GetViewByte(offset, out sectionAccessorIndex);
            pViewByte->Kind = ViewByteKind.Data;

            switch (kind)
            {
                //AnsiString
                case ViewKind.ImageBoundImportName:
                case ViewKind.ImageDelayLoadDescriptor_DllNameRVA:
                case ViewKind.ImageEnclaveImport_ImportName:
                case ViewKind.ImageExportDirectory_AddressOfNames_Name:
                case ViewKind.ImageExportDirectory_ForwarderName:
                case ViewKind.ImageExportDirectory_Name:
                case ViewKind.ImageImportDescriptor_Name:
                case ViewKind.SegmentName:
                case ViewKind.ShortImportLibrary_ImportName:
                case ViewKind.ShortImportLibrary_DllName:
                case ViewKind.AnsiString:

                //FixedAnsiString
                case ViewKind.LIBFile_Signature:

                //FixedUtf8String
                case ViewKind.DepsJson:
                case ViewKind.Manifest:
                case ViewKind.Metadata_String:
                case ViewKind.RuntimeConfigJson:
                case ViewKind.drectve:
                case ViewKind.Utf8String:

                //SymString
                case ViewKind.LibraryName:
                case ViewKind.NE_ImportedName_String:
                    pViewByte->DataKind = ViewByteDataKind.String;
                    break;

                case ViewKind.Metadata_Guid:
                case ViewKind.Guid:
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
                case ViewKind.NativeAOTModulesA:
                case ViewKind.NativeAOTModuleAddress:
                case ViewKind.NativeAOTModulesZ:
                case ViewKind.CodeViewSig:
                case ViewKind.NE_ModuleReference:
                case ViewKind.LockPrefixTable: //Array
                case ViewKind.TpiHashValues32: //Array
                case ViewKind.TpiHashValues16: //Array
                case ViewKind.TpiHashOffsets32: //Array
                case ViewKind.TpiHashOffsets16: //Array
                case ViewKind.SymbolOffsets: //Array
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
                case ViewKind.DNRB_Publics:
                case ViewKind.DNRB_SourceLines:
                case ViewKind.DNRB_Symbols:
                case ViewKind.DNRB_Types:
                case ViewKind.OldSymType:
                case ViewKind.OldTypType:
                    pViewByte->DataKind = ViewByteDataKind.Struct;
                    break;

                default:
                    if (kind >= ViewKind.drectve && kind <= ViewKind.UnknownSection)
                        pViewByte->DataKind = ViewByteDataKind.Integer;
                    else
                        throw new NotImplementedException();

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

                            case ViewKind.AnsiString:
                            case ViewKind.Utf8String:
                            case ViewKind.Utf16String:
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

        public override void WriteIL(long offset, NativeSpan<byte> ilBytes)
        {
            if (!TryGetViewOffset(offset, out offset))
                return;

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);

            pViewByte->Kind = ViewByteKind.Code;
            pViewByte->IsIL = true;

            for (var i = pViewByte + 1; i < pViewByte + ilBytes.Length; i++)
                i->Kind = ViewByteKind.Body;
        }

        public override ByteBlobView? WriteByteBlob(ByteBlob byteBlob)
        {
            if (!TryGetViewOffset(byteBlob.Offset, out var offset))
                return null;

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Integer; //Bytes
            _fileAccessor.AddStructKind(offset, byteBlob.viewKind);

            for (var i = pViewByte + 1; i < pViewByte + byteBlob.Bytes.Length; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }

        public override ByteBlobView? WritePadding(long offset, NativeSpan<byte> bytes)
        {
            if (!TryGetViewOffset(offset, out offset))
                return null;

            var pViewByte = _fileAccessor.GetViewByte(offset, out var sectionAccessorIndex);
            Debug.Assert(pViewByte->Kind != ViewByteKind.Body);
            pViewByte->Kind = ViewByteKind.Data;
            pViewByte->DataKind = ViewByteDataKind.Padding;

            for (var i = pViewByte + 1; i < pViewByte + bytes.Length; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }

        internal void RecordUnwindInfo(int rva)
        {
            _unwindInfos.Add(rva);
        }
    }
}
