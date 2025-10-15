using System.Runtime.CompilerServices;
using PESpy.View;

#nullable disable

namespace PESpy.PDB
{
    public sealed class ViewSymTypeDispatcher : SymTypeDispatcher<IView>
    {
        private ViewWriter viewWriter;

        internal ViewSymTypeDispatcher(ViewWriter viewWriter)
        {
            this.viewWriter = viewWriter;
        }

        protected override IView AlignSym(AlignSym value) => NewStruct(value);
        protected override IView AnnotationSym(AnnotationSym value) => NewStruct(value);
        protected override IView ArmSwitchTable(ArmSwitchTable value) => NewStruct(value);
        protected override IView AttrManyRegSym2(AttrManyRegSym2 value) => NewStruct(value);
        protected override IView AttrRegRel(AttrRegRel value) => NewStruct(value);
        protected override IView AttrRegSym(AttrRegSym value) => NewStruct(value);
        protected override IView AttrSlotSym(AttrSlotSym value) => NewStruct(value);
        protected override IView BlockSym16(BlockSym16 value) => NewStruct(value);
        protected override IView BlockSym32(BlockSym32 value) => NewStruct(value);
        protected override IView BPRelSym16(BPRelSym16 value) => NewStruct(value);
        protected override IView BPRelSym32(BPRelSym32 value) => NewStruct(value);
        protected override IView BPRelSym3216t(BPRelSym3216t value) => NewStruct(value);
        protected override IView BuildInfoSym(BuildInfoSym value) => NewStruct(value);
        protected override IView CallSiteInfo(CallSiteInfo value) => NewStruct(value);
        protected override IView CExMSym16(CExMSym16 value) => NewStruct(value);
        protected override IView CExMSym32(CExMSym32 value) => NewStruct(value);
        protected override IView CFlagSym(CFlagSym value) => NewStruct(value);
        protected override IView CoffGroupSym(CoffGroupSym value) => NewStruct(value);
        protected override IView CompileSym(CompileSym value) => NewStruct(value);
        protected override IView CompileSym3(CompileSym3 value) => NewStruct(value);
        protected override IView ConstSym(ConstSym value) => NewStruct(value);
        protected override IView ConstSym16t(ConstSym16t value) => NewStruct(value);
        protected override IView DataSym16(DataSym16 value) => NewStruct(value);
        protected override IView DataSym32(DataSym32 value) => NewStruct(value);
        protected override IView DataSym3216t(DataSym3216t value) => NewStruct(value);
        protected override IView DataSymHLSL(DataSymHLSL value) => NewStruct(value);
        protected override IView DataSymHLSL32(DataSymHLSL32 value) => NewStruct(value);
        protected override IView DataSymHLSL32Ex(DataSymHLSL32Ex value) => NewStruct(value);
        protected override IView DefRangeSym(DefRangeSym value) => NewStruct(value);
        protected override IView DefRangeSymFramePointerRel(DefRangeSymFramePointerRel value) => NewStruct(value);
        protected override IView DefRangeSymFramePointerRelFullScope(DefRangeSymFramePointerRelFullScope value) => NewStruct(value);
        protected override IView DefRangeSymHLSL(DefRangeSymHLSL value) => NewStruct(value);
        protected override IView DefRangeSymRegister(DefRangeSymRegister value) => NewStruct(value);
        protected override IView DefRangeSymRegisterRel(DefRangeSymRegisterRel value) => NewStruct(value);
        protected override IView DefRangeSymSubField(DefRangeSymSubField value) => NewStruct(value);
        protected override IView DefRangeSymSubfieldRegister(DefRangeSymSubfieldRegister value) => NewStruct(value);
        protected override IView DiscardedSym(DiscardedSym value) => NewStruct(value);
        protected override IView DPCSymTagMap(DPCSymTagMap value) => NewStruct(value);
        protected override IView EntryThisSym(EntryThisSym value) => NewStruct(value);
        protected override IView EnvBlockSym(EnvBlockSym value) => NewStruct(value);
        protected override IView ExportSym(ExportSym value) => NewStruct(value);
        protected override IView FileStaticSym(FileStaticSym value) => NewStruct(value);
        protected override IView FrameCookie(FrameCookie value) => NewStruct(value);
        protected override IView FrameProcSym(FrameProcSym value) => NewStruct(value);
        protected override IView FrameRelSym(FrameRelSym value) => NewStruct(value);
        protected override IView FunctionList(FunctionList value) => NewStruct(value);
        protected override IView HeapAllocSite(HeapAllocSite value) => NewStruct(value);
        protected override IView InlineSiteSym(InlineSiteSym value) => NewStruct(value);
        protected override IView InlineSiteSym2(InlineSiteSym2 value) => NewStruct(value);
        protected override IView LabelSym16(LabelSym16 value) => NewStruct(value);
        protected override IView LabelSym32(LabelSym32 value) => NewStruct(value);
        protected override IView LocalDPCGroupSharedSym(LocalDPCGroupSharedSym value) => NewStruct(value);
        protected override IView LocalSym(LocalSym value) => NewStruct(value);
        protected override IView ManProcSym(ManProcSym value) => NewStruct(value);
        protected override IView ManTypRef(ManTypRef value) => NewStruct(value);
        protected override IView ManyRegSym(ManyRegSym value) => NewStruct(value);
        protected override IView ManyRegSym16t(ManyRegSym16t value) => NewStruct(value);
        protected override IView ManyRegSym2(ManyRegSym2 value) => NewStruct(value);
        protected override IView ModTypeRef(ModTypeRef value) => NewStruct(value);
        protected override IView ObjNameSym(ObjNameSym value) => NewStruct(value);
        protected override IView OemSymbol(OemSymbol value) => NewStruct(value);
        protected override IView PdbMap(PdbMap value) => NewStruct(value);
        protected override IView PogoInfo(PogoInfo value) => NewStruct(value);
        protected override IView ProcSym16(ProcSym16 value) => NewStruct(value);
        protected override IView ProcSym32(ProcSym32 value) => NewStruct(value);
        protected override IView ProcSym3216t(ProcSym3216t value) => NewStruct(value);
        protected override IView ProcSymIA64(ProcSymIA64 value) => NewStruct(value);
        protected override IView ProcSymMips(ProcSymMips value) => NewStruct(value);
        protected override IView ProcSymMips16t(ProcSymMips16t value) => NewStruct(value);
        protected override IView PubSym32(PubSym32 value) => NewStruct(value);
        protected override IView RefMiniPdb(RefMiniPdb value) => NewStruct(value);
        protected override IView RefSym(RefSym value) => NewStruct(value);
        protected override IView RefSym2(RefSym2 value) => NewStruct(value);
        protected override IView RegRel16(RegRel16 value) => NewStruct(value);
        protected override IView RegRel32(RegRel32 value) => NewStruct(value);
        protected override IView RegRel3216t(RegRel3216t value) => NewStruct(value);
        protected override IView RegSym(RegSym value) => NewStruct(value);
        protected override IView RegSym16t(RegSym16t value) => NewStruct(value);
        protected override IView ReturnSym(ReturnSym value) => NewStruct(value);
        protected override IView SearchSym(SearchSym value) => NewStruct(value);
        protected override IView SectionSym(SectionSym value) => NewStruct(value);
        protected override IView SepCodeSym(SepCodeSym value) => NewStruct(value);
        protected override IView SLink32(SLink32 value) => NewStruct(value);
        protected override IView SlotSym32(SlotSym32 value) => NewStruct(value);
        protected override IView SymType(SymType value) => NewStruct(value);
        protected override IView ThunkSym16(ThunkSym16 value) => NewStruct(value);
        protected override IView ThunkSym32(ThunkSym32 value) => NewStruct(value);
        protected override IView TrampolineSym(TrampolineSym value) => NewStruct(value);
        protected override IView UdtSym(UdtSym value) => NewStruct(value);
        protected override IView UdtSym16t(UdtSym16t value) => NewStruct(value);
        protected override IView UNameSpace(UNameSpace value) => NewStruct(value);

        //Prevent boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IView NewStruct<T>(T value) where T : IViewable =>
            value.WriteStruct(viewWriter);
    }
}
