using System;
﻿using System.Collections.Generic;
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

        public PEViewByteViewWriter(PEFile peFile, FileAccessor fileAccessor, IFileDisassembler fileDisassembler, FileAnalyzer fileAnalyzer) : base(peFile)
        {
            _fileAccessor = fileAccessor;
            _fileDisassembler = fileDisassembler;
            _fileAnalyzer = fileAnalyzer;
        }

        protected internal override IView? NewUnmanagedStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            throw new System.NotImplementedException();
        }

        protected internal override unsafe IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            //Don't use FileAccessor.AddStruct here because we need to special case the body of IL methods

            if (_inRegion && FromRegion)
            {
                Debug.Assert(_currentRegion.End == value.Offset);
                _currentRegion.End += structSize;
            }

            //Every struct will call NewStruct(), so we want to take steps to minimize its size in NativeAOT

            var pViewByte = RegisterStruct(name, value.Offset, kind);

            switch (kind)
            {
                case ViewKind.ImageCorILMethodTiny:
                {
                    var val = value;
                    ProcessCorILMethodTiny(pViewByte, Unsafe.As<T, ImageCorILMethod>(ref val));
                    break;
                }    

                case ViewKind.ImageCorILMethodFat:
                {
                    var val = value;
                    ProcessCorILMethodFat(pViewByte, Unsafe.As<T, ImageCorILMethod>(ref val), structSize);
                    break;
                }

                default:
                    //All of the bytes are data
                    for (var i = pViewByte + 1; i < pViewByte + structSize; i++)
                        i->Kind = ViewByteKind.Body;
                    break;
            }

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

            _fileAccessor.AddXRef(structOffset + fieldOffset, targetOffset);
        }

        public override void WriteRVAXRef(int structOffset, int fieldOffset, int targetRVA)
        {
            if (targetRVA == 0)
                return;

            if (_fileAccessor.TryGetTargetAddress(targetRVA, out var targetAddress, out _))
                _fileAccessor.AddXRef(structOffset + fieldOffset, targetAddress);
        }

        public override void WriteVAXRef(int structOffset, int fieldOffset, int targetVA)
        {
            throw new System.NotImplementedException();
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

                        var alignment = (mainBodyEnd + 3) & ~3;

            for (var i = pILViewByte + 1; i < pILViewByte + ilBytes.Length; i++)
                i->Kind = ViewByteKind.Body;
        }

        private void ProcessCorILMethodFat(ViewByte* pViewByte, ImageCorILMethod value, int structSize)
        {
            //The first 12 bytes are data, then we have code, and then after that possibly also some EHSections

            for (var i = pViewByte + 1; i < pViewByte + 12; i++)
                i->Kind = ViewByteKind.Body;

            var ilBytes = value.ILBytes;

            var pILViewByte = pViewByte + 12;
            pILViewByte->Kind = ViewByteKind.Code;
            pILViewByte->IsIL = true;

            for (var i = pILViewByte + 1; i < pILViewByte + ilBytes.Length; i++)
                i->Kind = ViewByteKind.Body;

            if (value.EHSections.Length > 0)
            {
                var mainBodyEnd = 12 + ilBytes.Length;

                var alignment = (mainBodyEnd + 3) & ~3;

                if (alignment > 0)
                {
                    for (var i = pViewByte + mainBodyEnd; i < pViewByte + alignment; i++)
                    {
                        i->Kind = ViewByteKind.Data;
                        i->DataKind = ViewByteDataKind.Padding;
                    }
                }

                var ehSectionInfo = pViewByte + mainBodyEnd;
                ehSectionInfo->Kind = ViewByteKind.Data;
                var remainingBytes = structSize - mainBodyEnd;

                for (var i = ehSectionInfo + 1; i < ehSectionInfo + remainingBytes; i++)
                    ehSectionInfo->Kind = ViewByteKind.Body;
            }
        }

        protected internal override unsafe IView? NewValue<T>(int offset, in T value, int size, ViewKind kind, bool fromRegion)
        {
            return RegisterValue( offset, size, kind, fromRegion);
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

            var pViewByte = RegisterValueInternal(_fileAccessor, offset, size, kind, out _);

            for (var i = pViewByte + 1; i < pViewByte + size; i++)
                i->Kind = ViewByteKind.Body;

            return null;
        }

        internal static ViewByte* RegisterValueInternal(
            FileAccessor fileAccessor,
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

            return pViewByte;

            switch (kind)
            {
                //todo: certain data references like the reference to the security cookie do use the actual rva,
                //regardless of what the physical offset we're using is. this will cause an issue when attempting
                //to resolve the symbol reference. when we disassembled the code, we set the IP to the physical offset.
                //so some things are physical relative and others arent?
                case ViewKind.SecurityCookie:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__security_cookie);
                    break;

                case ViewKind.GuardCFCheckFunctionPointer:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_check_icall_fptr);
                    break;

                case ViewKind.GuardCFDispatchFunctionPointer:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_dispatch_icall_fptr);
                    break;

                case ViewKind.GuardCFFunctionTable:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_fids_table);
                    break;

                case ViewKind.GuardAddressTakenIatEntryTable:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_iat_table);
                    break;

                case ViewKind.GuardLongJumpTargetTable:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_longjmp_table);
                    break;

                case ViewKind.ImageEnclaveConfig:
                    _fileAccessor.AddName(offset, pViewByte, Strings.___enclave_config);
                    break;

                case ViewKind.GuardEHContinuationTable:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_eh_cont_table);
                    break;

                case ViewKind.GuardXFGCheckFunctionPointer:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_xfg_check_icall_fptr);
                    break;

                case ViewKind.GuardXFGDispatchFunctionPointer:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_xfg_dispatch_icall_fptr);
                    break;

                case ViewKind.GuardXFGTableDispatchFunctionPointer:
                    _fileAccessor.AddName(offset, pViewByte, Strings.__guard_xfg_table_dispatch_icall_fptr);
                    break;

                    /* Additional symbols we need to assign:
                     *     __safe_se_handler_table
                     *     @_guard_check_icall_nop@4
                     *     __guard_dispatch_icall_nop
                     *     __dynamic_value_reloc_table
                     *     __chpe_metadata
                     *     __guard_ss_verify_failure
                     *     __guard_ss_verify_failure_fptr
                     *     __guard_ss_verify_sp_fptr
                     *     __volatile_metadata
                     *     __guard_xfg_dispatch_icall_nop
                     *     __castguard_check_failure_os_handled_fptr
                     */
            }

            return null;
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

        public override ByteBlobView? WriteByteBlob(ByteBlob byteBlob)
        {
            return base.WriteByteBlob(byteBlob);
        }
    }
}
