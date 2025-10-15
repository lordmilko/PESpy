using System.Runtime.CompilerServices;
using PESpy.View;

#nullable disable

namespace PESpy.PDB
{
    internal class ViewTypTypeDispatcher : TypTypeDispatcher<IView>
    {
        private ViewWriter viewWriter;

        internal ViewTypTypeDispatcher(ViewWriter viewWriter)
        {
            this.viewWriter = viewWriter;
        }

        protected override IView LfAlias(LfAlias value) => NewStruct(value);
        protected override IView LfArgList(LfArgList value) => NewStruct(value);
        protected override IView LfArgList16t(LfArgList16t value) => NewStruct(value);
        protected override IView LfArray(LfArray value) => NewStruct(value);
        protected override IView LfArray16t(LfArray16t value) => NewStruct(value);
        protected override IView LfBArray(LfBArray value) => NewStruct(value);
        protected override IView LfBArray16t(LfBArray16t value) => NewStruct(value);
        protected override IView LfBClass(LfBClass value) => NewStruct(value);
        protected override IView LfBClass16t(LfBClass16t value) => NewStruct(value);
        protected override IView LfBitfield(LfBitfield value) => NewStruct(value);
        protected override IView LfBitfield16t(LfBitfield16t value) => NewStruct(value);
        protected override IView LfBuildInfo(LfBuildInfo value) => NewStruct(value);
        protected override IView LfChar(LfChar value) => NewStruct(value);
        protected override IView LfClass(LfClass value) => NewStruct(value);
        protected override IView LfClass16t(LfClass16t value) => NewStruct(value);
        protected override IView LfCmplx128(LfCmplx128 value) => NewStruct(value);
        protected override IView LfCmplx32(LfCmplx32 value) => NewStruct(value);
        protected override IView LfCmplx64(LfCmplx64 value) => NewStruct(value);
        protected override IView LfCmplx80(LfCmplx80 value) => NewStruct(value);
        protected override IView LfCobol0(LfCobol0 value) => NewStruct(value);
        protected override IView LfCobol016t(LfCobol016t value) => NewStruct(value);
        protected override IView LfCobol1(LfCobol1 value) => NewStruct(value);
        protected override IView LfDefArg(LfDefArg value) => NewStruct(value);
        protected override IView LfDefArg16t(LfDefArg16t value) => NewStruct(value);
        protected override IView LfDerived(LfDerived value) => NewStruct(value);
        protected override IView LfDerived16t(LfDerived16t value) => NewStruct(value);
        protected override IView LfDimArray(LfDimArray value) => NewStruct(value);
        protected override IView LfDimArray16t(LfDimArray16t value) => NewStruct(value);
        protected override IView LfDimCon(LfDimCon value) => NewStruct(value);
        protected override IView LfDimCon16t(LfDimCon16t value) => NewStruct(value);
        protected override IView LfDimVar(LfDimVar value) => NewStruct(value);
        protected override IView LfDimVar16t(LfDimVar16t value) => NewStruct(value);
        protected override IView LfEasy(LfEasy value) => NewStruct(value);
        protected override IView LfEndPreComp(LfEndPreComp value) => NewStruct(value);
        protected override IView LfEnum(LfEnum value) => NewStruct(value);
        protected override IView LfEnum16t(LfEnum16t value) => NewStruct(value);
        protected override IView LfEnumerate(LfEnumerate value) => NewStruct(value);
        protected override IView LfFieldList(LfFieldList value) => NewStruct(value);
        protected override IView LfFieldList16t(LfFieldList16t value) => NewStruct(value);
        protected override IView LfFriendCls(LfFriendCls value) => NewStruct(value);
        protected override IView LfFriendCls16t(LfFriendCls16t value) => NewStruct(value);
        protected override IView LfFriendFcn(LfFriendFcn value) => NewStruct(value);
        protected override IView LfFriendFcn16t(LfFriendFcn16t value) => NewStruct(value);
        protected override IView LfFuncId(LfFuncId value) => NewStruct(value);
        protected override IView LfHLSL(LfHLSL value) => NewStruct(value);
        protected override IView LfIndex(LfIndex value) => NewStruct(value);
        protected override IView LfIndex16t(LfIndex16t value) => NewStruct(value);
        protected override IView LfLabel(LfLabel value) => NewStruct(value);
        protected override IView LfList(LfList value) => NewStruct(value);
        protected override IView LfLong(LfLong value) => NewStruct(value);
        protected override IView LfManaged(LfManaged value) => NewStruct(value);
        protected override IView LfMatrix(LfMatrix value) => NewStruct(value);
        protected override IView LfMember(LfMember value) => NewStruct(value);
        protected override IView LfMember16t(LfMember16t value) => NewStruct(value);
        protected override IView LfMemberModify(LfMemberModify value) => NewStruct(value);
        protected override IView LfMethod(LfMethod value) => NewStruct(value);
        protected override IView LfMethod16t(LfMethod16t value) => NewStruct(value);
        protected override IView LfMethodList(LfMethodList value) => NewStruct(value);
        protected override IView LfMethodList16t(LfMethodList16t value) => NewStruct(value);
        protected override IView LfMFunc(LfMFunc value) => NewStruct(value);
        protected override IView LfMFunc16t(LfMFunc16t value) => NewStruct(value);
        protected override IView LfMFuncId(LfMFuncId value) => NewStruct(value);
        protected override IView LfModifier(LfModifier value) => NewStruct(value);
        protected override IView LfModifier16t(LfModifier16t value) => NewStruct(value);
        protected override IView LfModifierEx(LfModifierEx value) => NewStruct(value);
        protected override IView LfNestType(LfNestType value) => NewStruct(value);
        protected override IView LfNestType16t(LfNestType16t value) => NewStruct(value);
        protected override IView LfNestTypeEx(LfNestTypeEx value) => NewStruct(value);
        protected override IView LfOEM16t(LfOEM16t value) => NewStruct(value);
        protected override IView LfOneMethod(LfOneMethod value) => NewStruct(value);
        protected override IView LfOneMethod16t(LfOneMethod16t value) => NewStruct(value);
        protected override IView LfPointer(LfPointer value) => NewStruct(value);
        protected override IView LfPointer16t(LfPointer16t value) => NewStruct(value);
        protected override IView LfPreComp(LfPreComp value) => NewStruct(value);
        protected override IView LfPreComp16t(LfPreComp16t value) => NewStruct(value);
        protected override IView LfProc(LfProc value) => NewStruct(value);
        protected override IView LfProc16t(LfProc16t value) => NewStruct(value);
        protected override IView LfReal128(LfReal128 value) => NewStruct(value);
        protected override IView LfReal16(LfReal16 value) => NewStruct(value);
        protected override IView LfReal32(LfReal32 value) => NewStruct(value);
        protected override IView LfReal48(LfReal48 value) => NewStruct(value);
        protected override IView LfReal64(LfReal64 value) => NewStruct(value);
        protected override IView LfReal80(LfReal80 value) => NewStruct(value);
        protected override IView LfRefSym(LfRefSym value) => NewStruct(value);
        protected override IView LfShort(LfShort value) => NewStruct(value);
        protected override IView LfSkip(LfSkip value) => NewStruct(value);
        protected override IView LfSkip16t(LfSkip16t value) => NewStruct(value);
        protected override IView LfSTMember(LfSTMember value) => NewStruct(value);
        protected override IView LfSTMember16t(LfSTMember16t value) => NewStruct(value);
        protected override IView LfStringId(LfStringId value) => NewStruct(value);
        protected override IView LfTypeServer(LfTypeServer value) => NewStruct(value);
        protected override IView LfTypeServer2(LfTypeServer2 value) => NewStruct(value);
        protected override IView LfUdtModSrcLine(LfUdtModSrcLine value) => NewStruct(value);
        protected override IView LfUdtSrcLine(LfUdtSrcLine value) => NewStruct(value);
        protected override IView LfULong(LfULong value) => NewStruct(value);
        protected override IView LfUnion(LfUnion value) => NewStruct(value);
        protected override IView LfUnion16t(LfUnion16t value) => NewStruct(value);
        protected override IView LfUShort(LfUShort value) => NewStruct(value);
        protected override IView LfVarString(LfVarString value) => NewStruct(value);
        protected override IView LfVBClass(LfVBClass value) => NewStruct(value);
        protected override IView LfVBClass16t(LfVBClass16t value) => NewStruct(value);
        protected override IView LfVector(LfVector value) => NewStruct(value);
        protected override IView LfVftable(LfVftable value) => NewStruct(value);
        protected override IView LfVFTPath(LfVFTPath value) => NewStruct(value);
        protected override IView LfVFTPath16t(LfVFTPath16t value) => NewStruct(value);
        protected override IView LfVFuncOff(LfVFuncOff value) => NewStruct(value);
        protected override IView LfVFuncOff16t(LfVFuncOff16t value) => NewStruct(value);
        protected override IView LfVFuncTab(LfVFuncTab value) => NewStruct(value);
        protected override IView LfVFuncTab16t(LfVFuncTab16t value) => NewStruct(value);
        protected override IView LfVTShape(LfVTShape value) => NewStruct(value);

        //Prevent boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IView NewStruct<T>(T value) where T : IViewable =>
            value.WriteStruct(viewWriter);
    }
}
