using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public abstract class SymTypeDispatcher
    {
        public void Dispatch(SymType symType)
        {
            switch (symType.rectyp)
            {
                case SYM_ENUM_e.S_COMPILE:
                    CFlagSym((CFlagSym) symType);
                    break;

                case SYM_ENUM_e.S_REGISTER_16t:
                    RegSym16t((RegSym16t) symType);
                    break;

                case SYM_ENUM_e.S_CONSTANT_16t:
                    ConstSym16t((ConstSym16t) symType);
                    break;

                case SYM_ENUM_e.S_UDT_16t:
                case SYM_ENUM_e.S_COBOLUDT_16t:
                    UdtSym16t((UdtSym16t) symType);
                    break;

                case SYM_ENUM_e.S_SSEARCH:
                    SearchSym((SearchSym) symType);
                    break;

                case SYM_ENUM_e.S_END:
                case SYM_ENUM_e.S_SKIP:
                case SYM_ENUM_e.S_CVRESERVE:
                case SYM_ENUM_e.S_ENDARG:
                case SYM_ENUM_e.S_VFTABLE16:
                case SYM_ENUM_e.S_VFTABLE32_16t:
                case SYM_ENUM_e.S_VFTABLE32:
                case SYM_ENUM_e.S_RESERVED1:
                case SYM_ENUM_e.S_RESERVED2:
                case SYM_ENUM_e.S_RESERVED3:
                case SYM_ENUM_e.S_RESERVED4:
                case SYM_ENUM_e.S_LOCAL_2005:
                case SYM_ENUM_e.S_DEFRANGE_2005:
                case SYM_ENUM_e.S_DEFRANGE2_2005:
                case SYM_ENUM_e.S_INLINESITE_END:
                case SYM_ENUM_e.S_PROC_ID_END:
                    SymType((SymType) symType);
                    break;

                case SYM_ENUM_e.S_OBJNAME_ST:
                case SYM_ENUM_e.S_OBJNAME:
                    ObjNameSym((ObjNameSym) symType);
                    break;

                case SYM_ENUM_e.S_MANYREG_16t:
                    ManyRegSym16t((ManyRegSym16t) symType);
                    break;

                case SYM_ENUM_e.S_RETURN:
                    ReturnSym((ReturnSym) symType);
                    break;

                case SYM_ENUM_e.S_ENTRYTHIS:
                    EntryThisSym((EntryThisSym) symType);
                    break;

                case SYM_ENUM_e.S_BPREL16:
                    BPRelSym16((BPRelSym16) symType);
                    break;

                case SYM_ENUM_e.S_LDATA16:
                case SYM_ENUM_e.S_GDATA16:
                case SYM_ENUM_e.S_PUB16:
                    DataSym16((DataSym16) symType);
                    break;

                case SYM_ENUM_e.S_LPROC16:
                case SYM_ENUM_e.S_GPROC16:
                    ProcSym16((ProcSym16) symType);
                    break;

                case SYM_ENUM_e.S_THUNK16:
                    ThunkSym16((ThunkSym16) symType);
                    break;

                case SYM_ENUM_e.S_BLOCK16:
                case SYM_ENUM_e.S_WITH16:
                    BlockSym16((BlockSym16) symType);
                    break;

                case SYM_ENUM_e.S_LABEL16:
                    LabelSym16((LabelSym16) symType);
                    break;

                case SYM_ENUM_e.S_CEXMODEL16:
                    CExMSym16((CExMSym16) symType);
                    break;

                case SYM_ENUM_e.S_REGREL16:
                    RegRel16((RegRel16) symType);
                    break;

                case SYM_ENUM_e.S_BPREL32_16t:
                    BPRelSym3216t((BPRelSym3216t) symType);
                    break;

                case SYM_ENUM_e.S_LDATA32_16t:
                case SYM_ENUM_e.S_GDATA32_16t:
                case SYM_ENUM_e.S_PUB32_16t:
                case SYM_ENUM_e.S_LTHREAD32_16t:
                case SYM_ENUM_e.S_GTHREAD32_16t:
                    DataSym3216t((DataSym3216t) symType);
                    break;

                case SYM_ENUM_e.S_LPROC32_16t:
                case SYM_ENUM_e.S_GPROC32_16t:
                    ProcSym3216t((ProcSym3216t) symType);
                    break;

                case SYM_ENUM_e.S_THUNK32_ST:
                case SYM_ENUM_e.S_THUNK32:
                    ThunkSym32((ThunkSym32) symType);
                    break;

                case SYM_ENUM_e.S_BLOCK32_ST:
                case SYM_ENUM_e.S_WITH32_ST:
                case SYM_ENUM_e.S_BLOCK32:
                case SYM_ENUM_e.S_WITH32:
                    BlockSym32((BlockSym32) symType);
                    break;

                case SYM_ENUM_e.S_LABEL32_ST:
                case SYM_ENUM_e.S_LABEL32:
                    LabelSym32((LabelSym32) symType);
                    break;

                case SYM_ENUM_e.S_CEXMODEL32:
                    CExMSym32((CExMSym32) symType);
                    break;

                case SYM_ENUM_e.S_REGREL32_16t:
                    RegRel3216t((RegRel3216t) symType);
                    break;

                case SYM_ENUM_e.S_SLINK32:
                    SLink32((SLink32) symType);
                    break;

                case SYM_ENUM_e.S_LPROCMIPS_16t:
                case SYM_ENUM_e.S_GPROCMIPS_16t:
                    ProcSymMips16t((ProcSymMips16t) symType);
                    break;

                case SYM_ENUM_e.S_PROCREF_ST:
                case SYM_ENUM_e.S_DATAREF_ST:
                case SYM_ENUM_e.S_LPROCREF_ST:
                    RefSym((RefSym) symType);
                    break;

                case SYM_ENUM_e.S_ALIGN:
                    AlignSym((AlignSym) symType);
                    break;

                case SYM_ENUM_e.S_OEM:
                    OemSymbol((OemSymbol) symType);
                    break;

                case SYM_ENUM_e.S_REGISTER_ST:
                case SYM_ENUM_e.S_REGISTER:
                    RegSym((RegSym) symType);
                    break;

                case SYM_ENUM_e.S_CONSTANT_ST:
                case SYM_ENUM_e.S_CONSTANT:
                case SYM_ENUM_e.S_MANCONSTANT:
                    ConstSym((ConstSym) symType);
                    break;

                case SYM_ENUM_e.S_UDT_ST:
                case SYM_ENUM_e.S_COBOLUDT_ST:
                case SYM_ENUM_e.S_UDT:
                case SYM_ENUM_e.S_COBOLUDT:
                    UdtSym((UdtSym) symType);
                    break;

                case SYM_ENUM_e.S_MANYREG_ST:
                case SYM_ENUM_e.S_MANYREG:
                    ManyRegSym((ManyRegSym) symType);
                    break;

                case SYM_ENUM_e.S_BPREL32_ST:
                case SYM_ENUM_e.S_BPREL32:
                    BPRelSym32((BPRelSym32) symType);
                    break;

                case SYM_ENUM_e.S_LDATA32_ST:
                case SYM_ENUM_e.S_GDATA32_ST:
                case SYM_ENUM_e.S_LTHREAD32_ST:
                case SYM_ENUM_e.S_GTHREAD32_ST:
                case SYM_ENUM_e.S_LMANDATA_ST:
                case SYM_ENUM_e.S_GMANDATA_ST:
                case SYM_ENUM_e.S_LDATA32:
                case SYM_ENUM_e.S_GDATA32:
                case SYM_ENUM_e.S_LTHREAD32:
                case SYM_ENUM_e.S_GTHREAD32:
                case SYM_ENUM_e.S_LMANDATA:
                case SYM_ENUM_e.S_GMANDATA:
                    DataSym32((DataSym32) symType);
                    break;

                case SYM_ENUM_e.S_PUB32_ST:
                case SYM_ENUM_e.S_PUB32:
                    PubSym32((PubSym32) symType);
                    break;

                case SYM_ENUM_e.S_LPROC32_ST:
                case SYM_ENUM_e.S_GPROC32_ST:
                case SYM_ENUM_e.S_LPROC32:
                case SYM_ENUM_e.S_GPROC32:
                case SYM_ENUM_e.S_LPROC32_ID:
                case SYM_ENUM_e.S_GPROC32_ID:
                case SYM_ENUM_e.S_LPROC32_DPC:
                case SYM_ENUM_e.S_LPROC32_DPC_ID:
                    ProcSym32((ProcSym32) symType);
                    break;

                case SYM_ENUM_e.S_REGREL32_ST:
                case SYM_ENUM_e.S_REGREL32:
                case SYM_ENUM_e.S_REGREL32_ENCTMP:
                    RegRel32((RegRel32) symType);
                    break;

                case SYM_ENUM_e.S_LPROCMIPS_ST:
                case SYM_ENUM_e.S_GPROCMIPS_ST:
                case SYM_ENUM_e.S_LPROCMIPS:
                case SYM_ENUM_e.S_GPROCMIPS:
                case SYM_ENUM_e.S_LPROCMIPS_ID:
                case SYM_ENUM_e.S_GPROCMIPS_ID:
                    ProcSymMips((ProcSymMips) symType);
                    break;

                case SYM_ENUM_e.S_FRAMEPROC:
                    FrameProcSym((FrameProcSym) symType);
                    break;

                case SYM_ENUM_e.S_COMPILE2_ST:
                case SYM_ENUM_e.S_COMPILE2:
                    CompileSym((CompileSym) symType);
                    break;

                case SYM_ENUM_e.S_MANYREG2_ST:
                case SYM_ENUM_e.S_MANYREG2:
                    ManyRegSym2((ManyRegSym2) symType);
                    break;

                case SYM_ENUM_e.S_LPROCIA64_ST:
                case SYM_ENUM_e.S_GPROCIA64_ST:
                case SYM_ENUM_e.S_LPROCIA64:
                case SYM_ENUM_e.S_GPROCIA64:
                case SYM_ENUM_e.S_LPROCIA64_ID:
                case SYM_ENUM_e.S_GPROCIA64_ID:
                    ProcSymIA64((ProcSymIA64) symType);
                    break;

                case SYM_ENUM_e.S_LOCALSLOT_ST:
                case SYM_ENUM_e.S_PARAMSLOT_ST:
                case SYM_ENUM_e.S_LOCALSLOT:
                case SYM_ENUM_e.S_PARAMSLOT:
                    SlotSym32((SlotSym32) symType);
                    break;

                case SYM_ENUM_e.S_ANNOTATION:
                    AnnotationSym((AnnotationSym) symType);
                    break;

                case SYM_ENUM_e.S_GMANPROC_ST:
                case SYM_ENUM_e.S_LMANPROC_ST:
                case SYM_ENUM_e.S_GMANPROC:
                case SYM_ENUM_e.S_LMANPROC:
                    ManProcSym((ManProcSym) symType);
                    break;

                case SYM_ENUM_e.S_MANFRAMEREL_ST:
                case SYM_ENUM_e.S_MANFRAMEREL:
                case SYM_ENUM_e.S_ATTR_FRAMEREL:
                    FrameRelSym((FrameRelSym) symType);
                    break;

                case SYM_ENUM_e.S_MANREGISTER_ST:
                case SYM_ENUM_e.S_MANREGISTER:
                case SYM_ENUM_e.S_ATTR_REGISTER:
                    AttrRegSym((AttrRegSym) symType);
                    break;

                case SYM_ENUM_e.S_MANSLOT_ST:
                case SYM_ENUM_e.S_MANSLOT:
                    AttrSlotSym((AttrSlotSym) symType);
                    break;

                case SYM_ENUM_e.S_MANMANYREG_ST:
                case SYM_ENUM_e.S_MANMANYREG:
                    throw new NotImplementedException(); //todo

                case SYM_ENUM_e.S_MANREGREL_ST:
                case SYM_ENUM_e.S_MANREGREL:
                case SYM_ENUM_e.S_ATTR_REGREL:
                    AttrRegRel((AttrRegRel) symType);
                    break;

                case SYM_ENUM_e.S_MANMANYREG2_ST:
                case SYM_ENUM_e.S_MANMANYREG2:
                    throw new NotImplementedException(); //todo

                case SYM_ENUM_e.S_MANTYPREF:
                    ManTypRef((ManTypRef) symType);
                    break;

                case SYM_ENUM_e.S_UNAMESPACE_ST:
                case SYM_ENUM_e.S_UNAMESPACE:
                    UNameSpace((UNameSpace) symType);
                    break;

                case SYM_ENUM_e.S_PROCREF:
                case SYM_ENUM_e.S_DATAREF:
                case SYM_ENUM_e.S_LPROCREF:
                case SYM_ENUM_e.S_ANNOTATIONREF:
                case SYM_ENUM_e.S_TOKENREF:
                    RefSym2((RefSym2) symType);
                    break;

                case SYM_ENUM_e.S_TRAMPOLINE:
                    TrampolineSym((TrampolineSym) symType);
                    break;

                case SYM_ENUM_e.S_ATTR_MANYREG:
                    AttrManyRegSym2((AttrManyRegSym2) symType);
                    break;

                case SYM_ENUM_e.S_SEPCODE:
                    SepCodeSym((SepCodeSym) symType);
                    break;

                case SYM_ENUM_e.S_SECTION:
                    SectionSym((SectionSym) symType);
                    break;

                case SYM_ENUM_e.S_COFFGROUP:
                    CoffGroupSym((CoffGroupSym) symType);
                    break;

                case SYM_ENUM_e.S_EXPORT:
                    ExportSym((ExportSym) symType);
                    break;

                case SYM_ENUM_e.S_CALLSITEINFO:
                    CallSiteInfo((CallSiteInfo) symType);
                    break;

                case SYM_ENUM_e.S_FRAMECOOKIE:
                    FrameCookie((FrameCookie) symType);
                    break;

                case SYM_ENUM_e.S_DISCARDED:
                    DiscardedSym((DiscardedSym) symType);
                    break;

                case SYM_ENUM_e.S_COMPILE3:
                    CompileSym3((CompileSym3) symType);
                    break;

                case SYM_ENUM_e.S_ENVBLOCK:
                    EnvBlockSym((EnvBlockSym) symType);
                    break;

                case SYM_ENUM_e.S_LOCAL:
                    LocalSym((LocalSym) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE:
                    DefRangeSym((DefRangeSym) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_REGISTER:
                    DefRangeSymRegister((DefRangeSymRegister) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL:
                    DefRangeSymFramePointerRel((DefRangeSymFramePointerRel) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE:
                    DefRangeSymFramePointerRelFullScope((DefRangeSymFramePointerRelFullScope) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_SUBFIELD:
                    DefRangeSymSubField((DefRangeSymSubField) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_SUBFIELD_REGISTER:
                    DefRangeSymSubfieldRegister((DefRangeSymSubfieldRegister) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_REGISTER_REL:
                    DefRangeSymRegisterRel((DefRangeSymRegisterRel) symType);
                    break;

                case SYM_ENUM_e.S_BUILDINFO:
                    BuildInfoSym((BuildInfoSym) symType);
                    break;

                case SYM_ENUM_e.S_INLINESITE:
                    InlineSiteSym((InlineSiteSym) symType);
                    break;

                case SYM_ENUM_e.S_DEFRANGE_HLSL:
                case SYM_ENUM_e.S_DEFRANGE_DPC_PTR_TAG:
                    DefRangeSymHLSL((DefRangeSymHLSL) symType);
                    break;

                case SYM_ENUM_e.S_GDATA_HLSL:
                case SYM_ENUM_e.S_LDATA_HLSL:
                    DataSymHLSL((DataSymHLSL) symType);
                    break;

                case SYM_ENUM_e.S_FILESTATIC:
                    FileStaticSym((FileStaticSym) symType);
                    break;

                case SYM_ENUM_e.S_LOCAL_DPC_GROUPSHARED:
                    LocalDPCGroupSharedSym((LocalDPCGroupSharedSym) symType);
                    break;

                case SYM_ENUM_e.S_DPC_SYM_TAG_MAP:
                    DPCSymTagMap((DPCSymTagMap) symType);
                    break;

                case SYM_ENUM_e.S_ARMSWITCHTABLE:
                    ArmSwitchTable((ArmSwitchTable) symType);
                    break;

                case SYM_ENUM_e.S_CALLEES:
                case SYM_ENUM_e.S_CALLERS:
                    FunctionList((FunctionList) symType);
                    break;

                case SYM_ENUM_e.S_POGODATA:
                    PogoInfo((PogoInfo) symType);
                    break;

                case SYM_ENUM_e.S_INLINESITE2:
                    InlineSiteSym2((InlineSiteSym2) symType);
                    break;

                case SYM_ENUM_e.S_HEAPALLOCSITE:
                    HeapAllocSite((HeapAllocSite) symType);
                    break;

                case SYM_ENUM_e.S_MOD_TYPEREF:
                    ModTypeRef((ModTypeRef) symType);
                    break;

                case SYM_ENUM_e.S_REF_MINIPDB:
                    RefMiniPdb((RefMiniPdb) symType);
                    break;

                case SYM_ENUM_e.S_PDBMAP:
                    PdbMap((PdbMap) symType);
                    break;

                case SYM_ENUM_e.S_GDATA_HLSL32:
                case SYM_ENUM_e.S_LDATA_HLSL32:
                    DataSymHLSL32((DataSymHLSL32) symType);
                    break;

                case SYM_ENUM_e.S_GDATA_HLSL32_EX:
                case SYM_ENUM_e.S_LDATA_HLSL32_EX:
                    DataSymHLSL32Ex((DataSymHLSL32Ex) symType);
                    break;

                //Known unsupported types

                //We can't return SymType to the caller, as that will cause
                //the Visual Studio debugger to want to try and calculate the debug proxy
                //for the value again

                case SYM_ENUM_e.S_FRAMEREG:
                case SYM_ENUM_e.S_REF_MINIPDB2:
                case SYM_ENUM_e.S_INLINEES:
                case SYM_ENUM_e.S_HOTPATCHFUNC:
                case SYM_ENUM_e.S_BPREL32_INDIR:
                case SYM_ENUM_e.S_REGREL32_INDIR:
                case SYM_ENUM_e.S_GPROC32EX:
                case SYM_ENUM_e.S_LPROC32EX:
                case SYM_ENUM_e.S_GPROC32EX_ID:
                case SYM_ENUM_e.S_LPROC32EX_ID:
                case SYM_ENUM_e.S_STATICLOCAL:
                case SYM_ENUM_e.S_DEFRANGE_REGISTER_REL_INDIR:
                case SYM_ENUM_e.S_BPREL32_ENCTMP:
                case SYM_ENUM_e.S_BPREL32_INDIR_ENCTMP:
                case SYM_ENUM_e.S_REGREL32_INDIR_ENCTMP:
                case SYM_ENUM_e.S_ASSOCIATION:
                case SYM_ENUM_e.S_DEFRANGE_CONSTVAL_ON_ENTRY:
                case SYM_ENUM_e.S_DEFRANGE_GLOBALSYM_ON_ENTRY:
                    SymType((SymType) symType);
                    break;

                default:
                    Debug.Assert(false);
                    SymType((SymType) symType);
                    break;
            }
        }

        protected abstract void SymType(SymType value);

        protected abstract void CFlagSym(CFlagSym value);
        protected abstract void RegSym16t(RegSym16t value);
        protected abstract void ConstSym16t(ConstSym16t value);
        protected abstract void UdtSym16t(UdtSym16t value);
        protected abstract void SearchSym(SearchSym value);
        protected abstract void ObjNameSym(ObjNameSym value);
        protected abstract void ManyRegSym16t(ManyRegSym16t value);
        protected abstract void ReturnSym(ReturnSym value);
        protected abstract void EntryThisSym(EntryThisSym value);
        protected abstract void BPRelSym16(BPRelSym16 value);
        protected abstract void DataSym16(DataSym16 value);
        protected abstract void ProcSym16(ProcSym16 value);
        protected abstract void ThunkSym16(ThunkSym16 value);
        protected abstract void BlockSym16(BlockSym16 value);
        protected abstract void LabelSym16(LabelSym16 value);
        protected abstract void CExMSym16(CExMSym16 value);
        protected abstract void RegRel16(RegRel16 value);
        protected abstract void BPRelSym3216t(BPRelSym3216t value);
        protected abstract void DataSym3216t(DataSym3216t value);
        protected abstract void ProcSym3216t(ProcSym3216t value);
        protected abstract void ThunkSym32(ThunkSym32 value);
        protected abstract void BlockSym32(BlockSym32 value);
        protected abstract void LabelSym32(LabelSym32 value);
        protected abstract void CExMSym32(CExMSym32 value);
        protected abstract void RegRel3216t(RegRel3216t value);
        protected abstract void SLink32(SLink32 value);
        protected abstract void ProcSymMips16t(ProcSymMips16t value);
        protected abstract void RefSym(RefSym value);
        protected abstract void AlignSym(AlignSym value);
        protected abstract void OemSymbol(OemSymbol value);
        protected abstract void RegSym(RegSym value);
        protected abstract void ConstSym(ConstSym value);
        protected abstract void UdtSym(UdtSym value);
        protected abstract void ManyRegSym(ManyRegSym value);
        protected abstract void BPRelSym32(BPRelSym32 value);
        protected abstract void DataSym32(DataSym32 value);
        protected abstract void PubSym32(PubSym32 value);
        protected abstract void ProcSym32(ProcSym32 value);
        protected abstract void RegRel32(RegRel32 value);
        protected abstract void ProcSymMips(ProcSymMips value);
        protected abstract void FrameProcSym(FrameProcSym value);
        protected abstract void CompileSym(CompileSym value);
        protected abstract void ManyRegSym2(ManyRegSym2 value);
        protected abstract void ProcSymIA64(ProcSymIA64 value);
        protected abstract void SlotSym32(SlotSym32 value);
        protected abstract void AnnotationSym(AnnotationSym value);
        protected abstract void ManProcSym(ManProcSym value);
        protected abstract void FrameRelSym(FrameRelSym value);
        protected abstract void AttrRegSym(AttrRegSym value);
        protected abstract void AttrSlotSym(AttrSlotSym value);
        protected abstract void AttrRegRel(AttrRegRel value);
        protected abstract void ManTypRef(ManTypRef value);
        protected abstract void UNameSpace(UNameSpace value);
        protected abstract void RefSym2(RefSym2 value);
        protected abstract void TrampolineSym(TrampolineSym value);
        protected abstract void AttrManyRegSym2(AttrManyRegSym2 value);
        protected abstract void SepCodeSym(SepCodeSym value);
        protected abstract void SectionSym(SectionSym value);
        protected abstract void CoffGroupSym(CoffGroupSym value);
        protected abstract void ExportSym(ExportSym value);
        protected abstract void CallSiteInfo(CallSiteInfo value);
        protected abstract void FrameCookie(FrameCookie value);
        protected abstract void DiscardedSym(DiscardedSym value);
        protected abstract void CompileSym3(CompileSym3 value);
        protected abstract void EnvBlockSym(EnvBlockSym value);
        protected abstract void LocalSym(LocalSym value);
        protected abstract void DefRangeSym(DefRangeSym value);
        protected abstract void DefRangeSymRegister(DefRangeSymRegister value);
        protected abstract void DefRangeSymFramePointerRel(DefRangeSymFramePointerRel value);
        protected abstract void DefRangeSymFramePointerRelFullScope(DefRangeSymFramePointerRelFullScope value);
        protected abstract void DefRangeSymSubField(DefRangeSymSubField value);
        protected abstract void DefRangeSymSubfieldRegister(DefRangeSymSubfieldRegister value);
        protected abstract void DefRangeSymRegisterRel(DefRangeSymRegisterRel value);
        protected abstract void BuildInfoSym(BuildInfoSym value);
        protected abstract void InlineSiteSym(InlineSiteSym value);
        protected abstract void DefRangeSymHLSL(DefRangeSymHLSL value);
        protected abstract void DataSymHLSL(DataSymHLSL value);
        protected abstract void FileStaticSym(FileStaticSym value);
        protected abstract void LocalDPCGroupSharedSym(LocalDPCGroupSharedSym value);
        protected abstract void DPCSymTagMap(DPCSymTagMap value);
        protected abstract void ArmSwitchTable(ArmSwitchTable value);
        protected abstract void FunctionList(FunctionList value);
        protected abstract void PogoInfo(PogoInfo value);
        protected abstract void InlineSiteSym2(InlineSiteSym2 value);
        protected abstract void HeapAllocSite(HeapAllocSite value);
        protected abstract void ModTypeRef(ModTypeRef value);
        protected abstract void RefMiniPdb(RefMiniPdb value);
        protected abstract void PdbMap(PdbMap value);
        protected abstract void DataSymHLSL32(DataSymHLSL32 value);
        protected abstract void DataSymHLSL32Ex(DataSymHLSL32Ex value);
    }
}
