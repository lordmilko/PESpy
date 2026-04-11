using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Provides facilities for converting a symbol to another value, based on the type of
    /// symbol represented by a given <see cref="PESpy.PDB.SymType"/>.
    /// </summary>
    /// <typeparam name="T">The type of value that the symbol should be converted to.</typeparam>
    public abstract class SymTypeDispatcher<T>
    {
        public T Dispatch(SymType symType)
        {
            switch (symType.rectyp)
            {
                case SYM_ENUM_e.S_COMPILE:
                    return CFlagSym((CFlagSym) symType);

                case SYM_ENUM_e.S_REGISTER_16t:
                    return RegSym16t((RegSym16t) symType);

                case SYM_ENUM_e.S_CONSTANT_16t:
                    return ConstSym16t((ConstSym16t) symType);

                case SYM_ENUM_e.S_UDT_16t:
                case SYM_ENUM_e.S_COBOLUDT_16t:
                    return UdtSym16t((UdtSym16t) symType);

                case SYM_ENUM_e.S_SSEARCH:
                    return SearchSym((SearchSym) symType);

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
                    return SymType((SymType) symType);

                case SYM_ENUM_e.S_OBJNAME_ST:
                case SYM_ENUM_e.S_OBJNAME:
                    return ObjNameSym((ObjNameSym) symType);

                case SYM_ENUM_e.S_MANYREG_16t:
                    return ManyRegSym16t((ManyRegSym16t) symType);

                case SYM_ENUM_e.S_RETURN:
                    return ReturnSym((ReturnSym) symType);

                case SYM_ENUM_e.S_ENTRYTHIS:
                    return EntryThisSym((EntryThisSym) symType);

                case SYM_ENUM_e.S_BPREL16:
                    return BPRelSym16((BPRelSym16) symType);

                case SYM_ENUM_e.S_LDATA16:
                case SYM_ENUM_e.S_GDATA16:
                case SYM_ENUM_e.S_PUB16:
                    return DataSym16((DataSym16) symType);

                case SYM_ENUM_e.S_LPROC16:
                case SYM_ENUM_e.S_GPROC16:
                    return ProcSym16((ProcSym16) symType);

                case SYM_ENUM_e.S_THUNK16:
                    return ThunkSym16((ThunkSym16) symType);

                case SYM_ENUM_e.S_BLOCK16:
                case SYM_ENUM_e.S_WITH16:
                    return BlockSym16((BlockSym16) symType);

                case SYM_ENUM_e.S_LABEL16:
                    return LabelSym16((LabelSym16) symType);

                case SYM_ENUM_e.S_CEXMODEL16:
                    return CExMSym16((CExMSym16) symType);

                case SYM_ENUM_e.S_REGREL16:
                    return RegRel16((RegRel16) symType);

                case SYM_ENUM_e.S_BPREL32_16t:
                    return BPRelSym3216t((BPRelSym3216t) symType);

                case SYM_ENUM_e.S_LDATA32_16t:
                case SYM_ENUM_e.S_GDATA32_16t:
                case SYM_ENUM_e.S_PUB32_16t:
                case SYM_ENUM_e.S_LTHREAD32_16t:
                case SYM_ENUM_e.S_GTHREAD32_16t:
                    return DataSym3216t((DataSym3216t) symType);

                case SYM_ENUM_e.S_LPROC32_16t:
                case SYM_ENUM_e.S_GPROC32_16t:
                    return ProcSym3216t((ProcSym3216t) symType);

                case SYM_ENUM_e.S_THUNK32_ST:
                case SYM_ENUM_e.S_THUNK32:
                    return ThunkSym32((ThunkSym32) symType);

                case SYM_ENUM_e.S_BLOCK32_ST:
                case SYM_ENUM_e.S_WITH32_ST:
                case SYM_ENUM_e.S_BLOCK32:
                case SYM_ENUM_e.S_WITH32:
                    return BlockSym32((BlockSym32) symType);

                case SYM_ENUM_e.S_LABEL32_ST:
                case SYM_ENUM_e.S_LABEL32:
                    return LabelSym32((LabelSym32) symType);

                case SYM_ENUM_e.S_CEXMODEL32:
                    return CExMSym32((CExMSym32) symType);

                case SYM_ENUM_e.S_REGREL32_16t:
                    return RegRel3216t((RegRel3216t) symType);

                case SYM_ENUM_e.S_SLINK32:
                    return SLink32((SLink32) symType);

                case SYM_ENUM_e.S_LPROCMIPS_16t:
                case SYM_ENUM_e.S_GPROCMIPS_16t:
                    return ProcSymMips16t((ProcSymMips16t) symType);

                case SYM_ENUM_e.S_PROCREF_ST:
                case SYM_ENUM_e.S_DATAREF_ST:
                case SYM_ENUM_e.S_LPROCREF_ST:
                    return RefSym((RefSym) symType);

                case SYM_ENUM_e.S_ALIGN:
                    return AlignSym((AlignSym) symType);

                case SYM_ENUM_e.S_OEM:
                    return OemSymbol((OemSymbol) symType);

                case SYM_ENUM_e.S_REGISTER_ST:
                case SYM_ENUM_e.S_REGISTER:
                    return RegSym((RegSym) symType);

                case SYM_ENUM_e.S_CONSTANT_ST:
                case SYM_ENUM_e.S_CONSTANT:
                case SYM_ENUM_e.S_MANCONSTANT:
                    return ConstSym((ConstSym) symType);

                case SYM_ENUM_e.S_UDT_ST:
                case SYM_ENUM_e.S_COBOLUDT_ST:
                case SYM_ENUM_e.S_UDT:
                case SYM_ENUM_e.S_COBOLUDT:
                    return UdtSym((UdtSym) symType);

                case SYM_ENUM_e.S_MANYREG_ST:
                case SYM_ENUM_e.S_MANYREG:
                    return ManyRegSym((ManyRegSym) symType);

                case SYM_ENUM_e.S_BPREL32_ST:
                case SYM_ENUM_e.S_BPREL32:
                    return BPRelSym32((BPRelSym32) symType);

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
                    return DataSym32((DataSym32) symType);

                case SYM_ENUM_e.S_PUB32_ST:
                case SYM_ENUM_e.S_PUB32:
                    return PubSym32((PubSym32) symType);

                case SYM_ENUM_e.S_LPROC32_ST:
                case SYM_ENUM_e.S_GPROC32_ST:
                case SYM_ENUM_e.S_LPROC32:
                case SYM_ENUM_e.S_GPROC32:
                case SYM_ENUM_e.S_LPROC32_ID:
                case SYM_ENUM_e.S_GPROC32_ID:
                case SYM_ENUM_e.S_LPROC32_DPC:
                case SYM_ENUM_e.S_LPROC32_DPC_ID:
                    return ProcSym32((ProcSym32) symType);

                case SYM_ENUM_e.S_REGREL32_ST:
                case SYM_ENUM_e.S_REGREL32:
                case SYM_ENUM_e.S_REGREL32_ENCTMP:
                    return RegRel32((RegRel32) symType);

                case SYM_ENUM_e.S_LPROCMIPS_ST:
                case SYM_ENUM_e.S_GPROCMIPS_ST:
                case SYM_ENUM_e.S_LPROCMIPS:
                case SYM_ENUM_e.S_GPROCMIPS:
                case SYM_ENUM_e.S_LPROCMIPS_ID:
                case SYM_ENUM_e.S_GPROCMIPS_ID:
                    return ProcSymMips((ProcSymMips) symType);

                case SYM_ENUM_e.S_FRAMEPROC:
                    return FrameProcSym((FrameProcSym) symType);

                case SYM_ENUM_e.S_COMPILE2_ST:
                case SYM_ENUM_e.S_COMPILE2:
                    return CompileSym((CompileSym) symType);

                case SYM_ENUM_e.S_MANYREG2_ST:
                case SYM_ENUM_e.S_MANYREG2:
                    return ManyRegSym2((ManyRegSym2) symType);

                case SYM_ENUM_e.S_LPROCIA64_ST:
                case SYM_ENUM_e.S_GPROCIA64_ST:
                case SYM_ENUM_e.S_LPROCIA64:
                case SYM_ENUM_e.S_GPROCIA64:
                case SYM_ENUM_e.S_LPROCIA64_ID:
                case SYM_ENUM_e.S_GPROCIA64_ID:
                    return ProcSymIA64((ProcSymIA64) symType);

                case SYM_ENUM_e.S_LOCALSLOT_ST:
                case SYM_ENUM_e.S_PARAMSLOT_ST:
                case SYM_ENUM_e.S_LOCALSLOT:
                case SYM_ENUM_e.S_PARAMSLOT:
                    return SlotSym32((SlotSym32) symType);

                case SYM_ENUM_e.S_ANNOTATION:
                    return AnnotationSym((AnnotationSym) symType);

                case SYM_ENUM_e.S_GMANPROC_ST:
                case SYM_ENUM_e.S_LMANPROC_ST:
                case SYM_ENUM_e.S_GMANPROC:
                case SYM_ENUM_e.S_LMANPROC:
                    return ManProcSym((ManProcSym) symType);

                case SYM_ENUM_e.S_MANFRAMEREL_ST:
                case SYM_ENUM_e.S_MANFRAMEREL:
                case SYM_ENUM_e.S_ATTR_FRAMEREL:
                    return FrameRelSym((FrameRelSym) symType);

                case SYM_ENUM_e.S_MANREGISTER_ST:
                case SYM_ENUM_e.S_MANREGISTER:
                case SYM_ENUM_e.S_ATTR_REGISTER:
                    return AttrRegSym((AttrRegSym) symType);

                case SYM_ENUM_e.S_MANSLOT_ST:
                case SYM_ENUM_e.S_MANSLOT:
                    return AttrSlotSym((AttrSlotSym) symType);

                case SYM_ENUM_e.S_MANMANYREG_ST:
                case SYM_ENUM_e.S_MANMANYREG:
                    throw new NotImplementedException(); //todo

                case SYM_ENUM_e.S_MANREGREL_ST:
                case SYM_ENUM_e.S_MANREGREL:
                case SYM_ENUM_e.S_ATTR_REGREL:
                    return AttrRegRel((AttrRegRel) symType);

                case SYM_ENUM_e.S_MANMANYREG2_ST:
                case SYM_ENUM_e.S_MANMANYREG2:
                    throw new NotImplementedException(); //todo

                case SYM_ENUM_e.S_MANTYPREF:
                    return ManTypRef((ManTypRef) symType);

                case SYM_ENUM_e.S_UNAMESPACE_ST:
                case SYM_ENUM_e.S_UNAMESPACE:
                    return UNameSpace((UNameSpace) symType);

                case SYM_ENUM_e.S_PROCREF:
                case SYM_ENUM_e.S_DATAREF:
                case SYM_ENUM_e.S_LPROCREF:
                case SYM_ENUM_e.S_ANNOTATIONREF:
                case SYM_ENUM_e.S_TOKENREF:
                    return RefSym2((RefSym2) symType);

                case SYM_ENUM_e.S_TRAMPOLINE:
                    return TrampolineSym((TrampolineSym) symType);

                case SYM_ENUM_e.S_ATTR_MANYREG:
                    return AttrManyRegSym2((AttrManyRegSym2) symType);

                case SYM_ENUM_e.S_SEPCODE:
                    return SepCodeSym((SepCodeSym) symType);

                case SYM_ENUM_e.S_SECTION:
                    return SectionSym((SectionSym) symType);

                case SYM_ENUM_e.S_COFFGROUP:
                    return CoffGroupSym((CoffGroupSym) symType);

                case SYM_ENUM_e.S_EXPORT:
                    return ExportSym((ExportSym) symType);

                case SYM_ENUM_e.S_CALLSITEINFO:
                    return CallSiteInfo((CallSiteInfo) symType);

                case SYM_ENUM_e.S_FRAMECOOKIE:
                    return FrameCookie((FrameCookie) symType);

                case SYM_ENUM_e.S_DISCARDED:
                    return DiscardedSym((DiscardedSym) symType);

                case SYM_ENUM_e.S_COMPILE3:
                    return CompileSym3((CompileSym3) symType);

                case SYM_ENUM_e.S_ENVBLOCK:
                    return EnvBlockSym((EnvBlockSym) symType);

                case SYM_ENUM_e.S_LOCAL:
                    return LocalSym((LocalSym) symType);

                case SYM_ENUM_e.S_DEFRANGE:
                    return DefRangeSym((DefRangeSym) symType);

                case SYM_ENUM_e.S_DEFRANGE_REGISTER:
                    return DefRangeSymRegister((DefRangeSymRegister) symType);

                case SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL:
                    return DefRangeSymFramePointerRel((DefRangeSymFramePointerRel) symType);

                case SYM_ENUM_e.S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE:
                    return DefRangeSymFramePointerRelFullScope((DefRangeSymFramePointerRelFullScope) symType);

                case SYM_ENUM_e.S_DEFRANGE_SUBFIELD:
                    return DefRangeSymSubField((DefRangeSymSubField) symType);

                case SYM_ENUM_e.S_DEFRANGE_SUBFIELD_REGISTER:
                    return DefRangeSymSubfieldRegister((DefRangeSymSubfieldRegister) symType);

                case SYM_ENUM_e.S_DEFRANGE_REGISTER_REL:
                    return DefRangeSymRegisterRel((DefRangeSymRegisterRel) symType);

                case SYM_ENUM_e.S_BUILDINFO:
                    return BuildInfoSym((BuildInfoSym) symType);

                case SYM_ENUM_e.S_INLINESITE:
                    return InlineSiteSym((InlineSiteSym) symType);

                case SYM_ENUM_e.S_DEFRANGE_HLSL:
                case SYM_ENUM_e.S_DEFRANGE_DPC_PTR_TAG:
                    return DefRangeSymHLSL((DefRangeSymHLSL) symType);

                case SYM_ENUM_e.S_GDATA_HLSL:
                case SYM_ENUM_e.S_LDATA_HLSL:
                    return DataSymHLSL((DataSymHLSL) symType);

                case SYM_ENUM_e.S_FILESTATIC:
                    return FileStaticSym((FileStaticSym) symType);

                case SYM_ENUM_e.S_LOCAL_DPC_GROUPSHARED:
                    return LocalDPCGroupSharedSym((LocalDPCGroupSharedSym) symType);

                case SYM_ENUM_e.S_DPC_SYM_TAG_MAP:
                    return DPCSymTagMap((DPCSymTagMap) symType);

                case SYM_ENUM_e.S_ARMSWITCHTABLE:
                    return ArmSwitchTable((ArmSwitchTable) symType);

                case SYM_ENUM_e.S_CALLEES:
                case SYM_ENUM_e.S_CALLERS:
                    return FunctionList((FunctionList) symType);

                case SYM_ENUM_e.S_POGODATA:
                    return PogoInfo((PogoInfo) symType);

                case SYM_ENUM_e.S_INLINESITE2:
                    return InlineSiteSym2((InlineSiteSym2) symType);

                case SYM_ENUM_e.S_HEAPALLOCSITE:
                    return HeapAllocSite((HeapAllocSite) symType);

                case SYM_ENUM_e.S_MOD_TYPEREF:
                    return ModTypeRef((ModTypeRef) symType);

                case SYM_ENUM_e.S_REF_MINIPDB:
                    return RefMiniPdb((RefMiniPdb) symType);

                case SYM_ENUM_e.S_PDBMAP:
                    return PdbMap((PdbMap) symType);

                case SYM_ENUM_e.S_GDATA_HLSL32:
                case SYM_ENUM_e.S_LDATA_HLSL32:
                    return DataSymHLSL32((DataSymHLSL32) symType);

                case SYM_ENUM_e.S_GDATA_HLSL32_EX:
                case SYM_ENUM_e.S_LDATA_HLSL32_EX:
                    return DataSymHLSL32Ex((DataSymHLSL32Ex) symType);

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
                    return SymType((SymType) symType);

                default:
                    Debug.Assert(false);
                    return SymType((SymType) symType);
            }
        }

        protected abstract T SymType(SymType value);

        protected abstract T CFlagSym(CFlagSym value);
        protected abstract T RegSym16t(RegSym16t value);
        protected abstract T ConstSym16t(ConstSym16t value);
        protected abstract T UdtSym16t(UdtSym16t value);
        protected abstract T SearchSym(SearchSym value);
        protected abstract T ObjNameSym(ObjNameSym value);
        protected abstract T ManyRegSym16t(ManyRegSym16t value);
        protected abstract T ReturnSym(ReturnSym value);
        protected abstract T EntryThisSym(EntryThisSym value);
        protected abstract T BPRelSym16(BPRelSym16 value);
        protected abstract T DataSym16(DataSym16 value);
        protected abstract T ProcSym16(ProcSym16 value);
        protected abstract T ThunkSym16(ThunkSym16 value);
        protected abstract T BlockSym16(BlockSym16 value);
        protected abstract T LabelSym16(LabelSym16 value);
        protected abstract T CExMSym16(CExMSym16 value);
        protected abstract T RegRel16(RegRel16 value);
        protected abstract T BPRelSym3216t(BPRelSym3216t value);
        protected abstract T DataSym3216t(DataSym3216t value);
        protected abstract T ProcSym3216t(ProcSym3216t value);
        protected abstract T ThunkSym32(ThunkSym32 value);
        protected abstract T BlockSym32(BlockSym32 value);
        protected abstract T LabelSym32(LabelSym32 value);
        protected abstract T CExMSym32(CExMSym32 value);
        protected abstract T RegRel3216t(RegRel3216t value);
        protected abstract T SLink32(SLink32 value);
        protected abstract T ProcSymMips16t(ProcSymMips16t value);
        protected abstract T RefSym(RefSym value);
        protected abstract T AlignSym(AlignSym value);
        protected abstract T OemSymbol(OemSymbol value);
        protected abstract T RegSym(RegSym value);
        protected abstract T ConstSym(ConstSym value);
        protected abstract T UdtSym(UdtSym value);
        protected abstract T ManyRegSym(ManyRegSym value);
        protected abstract T BPRelSym32(BPRelSym32 value);
        protected abstract T DataSym32(DataSym32 value);
        protected abstract T PubSym32(PubSym32 value);
        protected abstract T ProcSym32(ProcSym32 value);
        protected abstract T RegRel32(RegRel32 value);
        protected abstract T ProcSymMips(ProcSymMips value);
        protected abstract T FrameProcSym(FrameProcSym value);
        protected abstract T CompileSym(CompileSym value);
        protected abstract T ManyRegSym2(ManyRegSym2 value);
        protected abstract T ProcSymIA64(ProcSymIA64 value);
        protected abstract T SlotSym32(SlotSym32 value);
        protected abstract T AnnotationSym(AnnotationSym value);
        protected abstract T ManProcSym(ManProcSym value);
        protected abstract T FrameRelSym(FrameRelSym value);
        protected abstract T AttrRegSym(AttrRegSym value);
        protected abstract T AttrSlotSym(AttrSlotSym value);
        protected abstract T AttrRegRel(AttrRegRel value);
        protected abstract T ManTypRef(ManTypRef value);
        protected abstract T UNameSpace(UNameSpace value);
        protected abstract T RefSym2(RefSym2 value);
        protected abstract T TrampolineSym(TrampolineSym value);
        protected abstract T AttrManyRegSym2(AttrManyRegSym2 value);
        protected abstract T SepCodeSym(SepCodeSym value);
        protected abstract T SectionSym(SectionSym value);
        protected abstract T CoffGroupSym(CoffGroupSym value);
        protected abstract T ExportSym(ExportSym value);
        protected abstract T CallSiteInfo(CallSiteInfo value);
        protected abstract T FrameCookie(FrameCookie value);
        protected abstract T DiscardedSym(DiscardedSym value);
        protected abstract T CompileSym3(CompileSym3 value);
        protected abstract T EnvBlockSym(EnvBlockSym value);
        protected abstract T LocalSym(LocalSym value);
        protected abstract T DefRangeSym(DefRangeSym value);
        protected abstract T DefRangeSymRegister(DefRangeSymRegister value);
        protected abstract T DefRangeSymFramePointerRel(DefRangeSymFramePointerRel value);
        protected abstract T DefRangeSymFramePointerRelFullScope(DefRangeSymFramePointerRelFullScope value);
        protected abstract T DefRangeSymSubField(DefRangeSymSubField value);
        protected abstract T DefRangeSymSubfieldRegister(DefRangeSymSubfieldRegister value);
        protected abstract T DefRangeSymRegisterRel(DefRangeSymRegisterRel value);
        protected abstract T BuildInfoSym(BuildInfoSym value);
        protected abstract T InlineSiteSym(InlineSiteSym value);
        protected abstract T DefRangeSymHLSL(DefRangeSymHLSL value);
        protected abstract T DataSymHLSL(DataSymHLSL value);
        protected abstract T FileStaticSym(FileStaticSym value);
        protected abstract T LocalDPCGroupSharedSym(LocalDPCGroupSharedSym value);
        protected abstract T DPCSymTagMap(DPCSymTagMap value);
        protected abstract T ArmSwitchTable(ArmSwitchTable value);
        protected abstract T FunctionList(FunctionList value);
        protected abstract T PogoInfo(PogoInfo value);
        protected abstract T InlineSiteSym2(InlineSiteSym2 value);
        protected abstract T HeapAllocSite(HeapAllocSite value);
        protected abstract T ModTypeRef(ModTypeRef value);
        protected abstract T RefMiniPdb(RefMiniPdb value);
        protected abstract T PdbMap(PdbMap value);
        protected abstract T DataSymHLSL32(DataSymHLSL32 value);
        protected abstract T DataSymHLSL32Ex(DataSymHLSL32Ex value);
    }
}
