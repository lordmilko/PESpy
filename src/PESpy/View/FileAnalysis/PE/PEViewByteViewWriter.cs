using System.Runtime.CompilerServices;

namespace PESpy.View
{
    internal unsafe class PEViewByteViewWriter : PEViewWriter
    {
        private readonly FileAccessor _fileAccessor;
        private readonly IFileDisassembler? _fileDisassembler;

        public PEViewByteViewWriter(PEFile peFile, FileAccessor fileAccessor, IFileDisassembler fileDisassembler) : base(peFile)
        {
            _fileAccessor = fileAccessor;
            _fileDisassembler = fileDisassembler;
        }

        protected internal override unsafe IView? NewStruct<T>(FixedUtf8String name, in T value, ViewKind kind, int structSize)
        {
            //Don't use FileAccessor.AddStruct here because we need to special case the body of IL methods

            var pViewByte = _fileAccessor.GetViewByte(value.Offset, out _);
            pViewByte->Kind = ViewByteKind.Data;
            _fileAccessor.AddName(value.Offset, pViewByte, name);
            _fileAccessor.AddStructKind(value.Offset, kind);

            switch (kind)
            {
                case ViewKind.ImageCorILMethodTiny:
                {
                    //The first byte is data, but all of the bytes after it are code

                    var val = value;

                    var ilBytes = Unsafe.As<T, ImageCorILMethod>(ref val).ILBytes;

                    var pILViewByte = pViewByte + 1;
                    pILViewByte->Kind = ViewByteKind.Code;
                    pILViewByte->IsIL = true;

                    //todo: need to queue up the fact we need to apply the name to this item

                    for (var i = pILViewByte + 1; i < pILViewByte + ilBytes.Length; i++)
                        i->Kind = ViewByteKind.Body;

                    break;
                }

                case ViewKind.ImageCorILMethodFat:
                {
                    //The first 12 bytes are data, then we have code, and then after that possibly also some EHSections

                    for (var i = pViewByte + 1; i < pViewByte + 12; i++)
                        i->Kind = ViewByteKind.Body;

                    var val = value;

                    var ilMethod = Unsafe.As<T, ImageCorILMethod>(ref val);
                    var ilBytes = ilMethod.ILBytes;

                    var pILViewByte = pViewByte + 12;
                    pILViewByte->Kind = ViewByteKind.Code;
                    pILViewByte->IsIL = true;

                    for (var i = pILViewByte + 1; i < pILViewByte + ilBytes.Length; i++)
                        i->Kind = ViewByteKind.Body;

                    if (ilMethod.EHSections.Length > 0)
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

        protected internal override unsafe IView? NewValue<T>(int offset, in T value, int size, ViewKind kind)
        {
            _fileAccessor.AddStructKind(offset, kind);

            var pViewByte = _fileAccessor.GetViewByte(offset, out _);
            pViewByte->Kind = ViewByteKind.Data;

            for (var i = pViewByte + 1; i < pViewByte + size; i++)
                i->Kind = ViewByteKind.Body;

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

        public override void WriteDosStub(in ByteBlob byteBlob) =>
            _fileDisassembler?.WriteDosStub(_fileAccessor, byteBlob);

        public override ByteBlobView? WriteByteBlob(ByteBlob byteBlob)
        {
            return base.WriteByteBlob(byteBlob);
        }
    }
}
