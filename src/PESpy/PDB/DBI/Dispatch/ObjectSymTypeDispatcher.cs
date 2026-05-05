#nullable disable

namespace PESpy.PDB
{
    /// <summary>
    /// Provides facilities for converting a <see cref="PESpy.PDB.SymType"/> to its true
    /// underlying symbol type, and returning that value as an <see cref="object"/>.
    /// </summary>
    public sealed class ObjectSymTypeDispatcher : SymTypeDispatcher<object>
    {
        public static readonly ObjectSymTypeDispatcher Instance = new();

        protected override object AlignSym(AlignSym value) => value;
        protected override object AnnotationSym(AnnotationSym value) => value;
        protected override object ArmSwitchTable(ArmSwitchTable value) => value;
        protected override object AttrManyRegSym2(AttrManyRegSym2 value) => value;
        protected override object AttrRegRel(AttrRegRel value) => value;
        protected override object AttrRegSym(AttrRegSym value) => value;
        protected override object AttrSlotSym(AttrSlotSym value) => value;
        protected override object BlockSym16(BlockSym16 value) => value;
        protected override object BlockSym32(BlockSym32 value) => value;
        protected override object BPRelSym16(BPRelSym16 value) => value;
        protected override object BPRelSym32(BPRelSym32 value) => value;
        protected override object BPRelSym3216t(BPRelSym3216t value) => value;
        protected override object BuildInfoSym(BuildInfoSym value) => value;
        protected override object CallSiteInfo(CallSiteInfo value) => value;
        protected override object CExMSym16(CExMSym16 value) => value;
        protected override object CExMSym32(CExMSym32 value) => value;
        protected override object CFlagSym(CFlagSym value) => value;
        protected override object CoffGroupSym(CoffGroupSym value) => value;
        protected override object CompileSym(CompileSym value) => value;
        protected override object CompileSym3(CompileSym3 value) => value;
        protected override object ConstSym(ConstSym value) => value;
        protected override object ConstSym16t(ConstSym16t value) => value;
        protected override object DataSym16(DataSym16 value) => value;
        protected override object DataSym32(DataSym32 value) => value;
        protected override object DataSym3216t(DataSym3216t value) => value;
        protected override object DataSymHLSL(DataSymHLSL value) => value;
        protected override object DataSymHLSL32(DataSymHLSL32 value) => value;
        protected override object DataSymHLSL32Ex(DataSymHLSL32Ex value) => value;
        protected override object DefRangeSym(DefRangeSym value) => value;
        protected override object DefRangeSymFramePointerRel(DefRangeSymFramePointerRel value) => value;
        protected override object DefRangeSymFramePointerRelFullScope(DefRangeSymFramePointerRelFullScope value) => value;
        protected override object DefRangeSymHLSL(DefRangeSymHLSL value) => value;
        protected override object DefRangeSymRegister(DefRangeSymRegister value) => value;
        protected override object DefRangeSymRegisterRel(DefRangeSymRegisterRel value) => value;
        protected override object DefRangeSymSubField(DefRangeSymSubField value) => value;
        protected override object DefRangeSymSubfieldRegister(DefRangeSymSubfieldRegister value) => value;
        protected override object DiscardedSym(DiscardedSym value) => value;
        protected override object DPCSymTagMap(DPCSymTagMap value) => value;
        protected override object EntryThisSym(EntryThisSym value) => value;
        protected override object EnvBlockSym(EnvBlockSym value) => value;
        protected override object ExportSym(ExportSym value) => value;
        protected override object FileStaticSym(FileStaticSym value) => value;
        protected override object FrameCookie(FrameCookie value) => value;
        protected override object FrameProcSym(FrameProcSym value) => value;
        protected override object FrameRelSym(FrameRelSym value) => value;
        protected override object FunctionList(FunctionList value) => value;
        protected override object HeapAllocSite(HeapAllocSite value) => value;
        protected override object InlineSiteSym(InlineSiteSym value) => value;
        protected override object InlineSiteSym2(InlineSiteSym2 value) => value;
        protected override object LabelSym16(LabelSym16 value) => value;
        protected override object LabelSym32(LabelSym32 value) => value;
        protected override object LocalDPCGroupSharedSym(LocalDPCGroupSharedSym value) => value;
        protected override object LocalSym(LocalSym value) => value;
        protected override object ManProcSym(ManProcSym value) => value;
        protected override object ManTypRef(ManTypRef value) => value;
        protected override object ManyRegSym(ManyRegSym value) => value;
        protected override object ManyRegSym16t(ManyRegSym16t value) => value;
        protected override object ManyRegSym2(ManyRegSym2 value) => value;
        protected override object ModTypeRef(ModTypeRef value) => value;
        protected override object ObjNameSym(ObjNameSym value) => value;
        protected override object OemSymbol(OemSymbol value) => value;
        protected override object PdbMap(PdbMap value) => value;
        protected override object PogoInfo(PogoInfo value) => value;
        protected override object ProcSym16(ProcSym16 value) => value;
        protected override object ProcSym32(ProcSym32 value) => value;
        protected override object ProcSym3216t(ProcSym3216t value) => value;
        protected override object ProcSymIA64(ProcSymIA64 value) => value;
        protected override object ProcSymMips(ProcSymMips value) => value;
        protected override object ProcSymMips16t(ProcSymMips16t value) => value;
        protected override object PubSym32(PubSym32 value) => value;
        protected override object RefMiniPdb(RefMiniPdb value) => value;
        protected override object RefSym(RefSym value) => value;
        protected override object RefSym2(RefSym2 value) => value;
        protected override object RegRel16(RegRel16 value) => value;
        protected override object RegRel32(RegRel32 value) => value;
        protected override object RegRel3216t(RegRel3216t value) => value;
        protected override object RegSym(RegSym value) => value;
        protected override object RegSym16t(RegSym16t value) => value;
        protected override object ReturnSym(ReturnSym value) => value;
        protected override object SearchSym(SearchSym value) => value;
        protected override object SectionSym(SectionSym value) => value;
        protected override object SepCodeSym(SepCodeSym value) => value;
        protected override object SLink32(SLink32 value) => value;
        protected override object SlotSym32(SlotSym32 value) => value;
        protected override object SymType(SymType value) => value;
        protected override object ThunkSym16(ThunkSym16 value) => value;
        protected override object ThunkSym32(ThunkSym32 value) => value;
        protected override object TrampolineSym(TrampolineSym value) => value;
        protected override object UdtSym(UdtSym value) => value;
        protected override object UdtSym16t(UdtSym16t value) => value;
        protected override object UNameSpace(UNameSpace value) => value;
    }
}
