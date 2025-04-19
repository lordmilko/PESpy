using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    [DebuggerTypeProxy(typeof(TypTypeProxy))]
    [DebuggerDisplay("{TypTypeProxy.DebuggerDisplay(this),nq}")]
    public readonly unsafe struct TypType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly TYPTYPE* value;

        public ushort len => value->len;
        public LEAF_ENUM_e leaf => value->leaf;

        public TypType(TYPTYPE* value)
        {
            this.value = value;
        }

        public override string ToString() => TypTypeProxy.GetString(this);

        internal static void AssertMissing(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        internal static FixedUtf8String ReadString(byte* ptr)
        {
            throw new System.NotImplementedException();
        }

        public static implicit operator TypType(TYPTYPE* value) => new TypType(value);

        public static implicit operator LfAlias(TypType typType) => new LfAlias((lfAlias*) ((byte*) typType.value + 2));
        public static implicit operator LfArgList(TypType typType) => new LfArgList((lfArgList*) ((byte*) typType.value + 2));
        public static implicit operator LfArgList16t(TypType typType) => new LfArgList16t((lfArgList_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfArray(TypType typType) => new LfArray((lfArray*) ((byte*) typType.value + 2));
        public static implicit operator LfArray16t(TypType typType) => new LfArray16t((lfArray_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfBArray(TypType typType) => new LfBArray((lfBArray*) ((byte*) typType.value + 2));
        public static implicit operator LfBArray16t(TypType typType) => new LfBArray16t((lfBArray_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfBClass(TypType typType) => new LfBClass((lfBClass*) ((byte*) typType.value + 2));
        public static implicit operator LfBClass16t(TypType typType) => new LfBClass16t((lfBClass_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfBitfield(TypType typType) => new LfBitfield((lfBitfield*) ((byte*) typType.value + 2));
        public static implicit operator LfBitfield16t(TypType typType) => new LfBitfield16t((lfBitfield_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfBuildInfo(TypType typType) => new LfBuildInfo((lfBuildInfo*) ((byte*) typType.value + 2));
        public static implicit operator LfChar(TypType typType) => new LfChar((lfChar*) ((byte*) typType.value + 2));
        public static implicit operator LfClass(TypType typType) => new LfClass((lfClass*) ((byte*) typType.value + 2));
        public static implicit operator LfClass16t(TypType typType) => new LfClass16t((lfClass_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfCmplx128(TypType typType) => new LfCmplx128((lfCmplx128*) ((byte*) typType.value + 2));
        public static implicit operator LfCmplx32(TypType typType) => new LfCmplx32((lfCmplx32*) ((byte*) typType.value + 2));
        public static implicit operator LfCmplx64(TypType typType) => new LfCmplx64((lfCmplx64*) ((byte*) typType.value + 2));
        public static implicit operator LfCmplx80(TypType typType) => new LfCmplx80((lfCmplx80*) ((byte*) typType.value + 2));
        public static implicit operator LfCobol0(TypType typType) => new LfCobol0((lfCobol0*) ((byte*) typType.value + 2));
        public static implicit operator LfCobol016t(TypType typType) => new LfCobol016t((lfCobol0_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfCobol1(TypType typType) => new LfCobol1((lfCobol1*) ((byte*) typType.value + 2));
        public static implicit operator LfDefArg(TypType typType) => new LfDefArg((lfDefArg*) ((byte*) typType.value + 2));
        public static implicit operator LfDefArg16t(TypType typType) => new LfDefArg16t((lfDefArg_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfDerived(TypType typType) => new LfDerived((lfDerived*) ((byte*) typType.value + 2));
        public static implicit operator LfDerived16t(TypType typType) => new LfDerived16t((lfDerived_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfDimArray(TypType typType) => new LfDimArray((lfDimArray*) ((byte*) typType.value + 2));
        public static implicit operator LfDimArray16t(TypType typType) => new LfDimArray16t((lfDimArray_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfDimCon(TypType typType) => new LfDimCon((lfDimCon*) ((byte*) typType.value + 2));
        public static implicit operator LfDimCon16t(TypType typType) => new LfDimCon16t((lfDimCon_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfDimVar(TypType typType) => new LfDimVar((lfDimVar*) ((byte*) typType.value + 2));
        public static implicit operator LfDimVar16t(TypType typType) => new LfDimVar16t((lfDimVar_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfEasy(TypType typType) => new LfEasy((lfEasy*) ((byte*) typType.value + 2));
        public static implicit operator LfEndPreComp(TypType typType) => new LfEndPreComp((lfEndPreComp*) ((byte*) typType.value + 2));
        public static implicit operator LfEnum(TypType typType) => new LfEnum((lfEnum*) ((byte*) typType.value + 2));
        public static implicit operator LfEnum16t(TypType typType) => new LfEnum16t((lfEnum_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfEnumerate(TypType typType) => new LfEnumerate((lfEnumerate*) ((byte*) typType.value + 2));
        public static implicit operator LfFieldList(TypType typType) => new LfFieldList((lfFieldList*) ((byte*) typType.value + 2));
        public static implicit operator LfFieldList16t(TypType typType) => new LfFieldList16t((lfFieldList_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfFriendCls(TypType typType) => new LfFriendCls((lfFriendCls*) ((byte*) typType.value + 2));
        public static implicit operator LfFriendCls16t(TypType typType) => new LfFriendCls16t((lfFriendCls_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfFriendFcn(TypType typType) => new LfFriendFcn((lfFriendFcn*) ((byte*) typType.value + 2));
        public static implicit operator LfFriendFcn16t(TypType typType) => new LfFriendFcn16t((lfFriendFcn_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfFuncId(TypType typType) => new LfFuncId((lfFuncId*) ((byte*) typType.value + 2));
        public static implicit operator LfHLSL(TypType typType) => new LfHLSL((lfHLSL*) ((byte*) typType.value + 2));
        public static implicit operator LfIndex(TypType typType) => new LfIndex((lfIndex*) ((byte*) typType.value + 2));
        public static implicit operator LfIndex16t(TypType typType) => new LfIndex16t((lfIndex_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfLabel(TypType typType) => new LfLabel((lfLabel*) ((byte*) typType.value + 2));
        public static implicit operator LfList(TypType typType) => new LfList((lfList*) ((byte*) typType.value + 2));
        public static implicit operator LfLong(TypType typType) => new LfLong((lfLong*) ((byte*) typType.value + 2));
        public static implicit operator LfManaged(TypType typType) => new LfManaged((lfManaged*) ((byte*) typType.value + 2));
        public static implicit operator LfMatrix(TypType typType) => new LfMatrix((lfMatrix*) ((byte*) typType.value + 2));
        public static implicit operator LfMember(TypType typType) => new LfMember((lfMember*) ((byte*) typType.value + 2));
        public static implicit operator LfMember16t(TypType typType) => new LfMember16t((lfMember_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfMemberModify(TypType typType) => new LfMemberModify((lfMemberModify*) ((byte*) typType.value + 2));
        public static implicit operator LfMethod(TypType typType) => new LfMethod((lfMethod*) ((byte*) typType.value + 2));
        public static implicit operator LfMethod16t(TypType typType) => new LfMethod16t((lfMethod_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfMethodList(TypType typType) => new LfMethodList((lfMethodList*) ((byte*) typType.value + 2));
        public static implicit operator LfMethodList16t(TypType typType) => new LfMethodList16t((lfMethodList_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfMFunc(TypType typType) => new LfMFunc((lfMFunc*) ((byte*) typType.value + 2));
        public static implicit operator LfMFunc16t(TypType typType) => new LfMFunc16t((lfMFunc_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfMFuncId(TypType typType) => new LfMFuncId((lfMFuncId*) ((byte*) typType.value + 2));
        public static implicit operator LfModifier(TypType typType) => new LfModifier((lfModifier*) ((byte*) typType.value + 2));
        public static implicit operator LfModifier16t(TypType typType) => new LfModifier16t((lfModifier_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfModifierEx(TypType typType) => new LfModifierEx((lfModifierEx*) ((byte*) typType.value + 2));
        public static implicit operator LfNestType(TypType typType) => new LfNestType((lfNestType*) ((byte*) typType.value + 2));
        public static implicit operator LfNestType16t(TypType typType) => new LfNestType16t((lfNestType_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfNestTypeEx(TypType typType) => new LfNestTypeEx((lfNestTypeEx*) ((byte*) typType.value + 2));
        public static implicit operator LfOct(TypType typType) => new LfOct((lfOct*) ((byte*) typType.value + 2));
        public static implicit operator LfOEM(TypType typType) => new LfOEM((lfOEM*) ((byte*) typType.value + 2));
        public static implicit operator LfOEM16t(TypType typType) => new LfOEM16t((lfOEM_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfOEM2(TypType typType) => new LfOEM2((lfOEM2*) ((byte*) typType.value + 2));
        public static implicit operator LfOneMethod(TypType typType) => new LfOneMethod((lfOneMethod*) ((byte*) typType.value + 2));
        public static implicit operator LfOneMethod16t(TypType typType) => new LfOneMethod16t((lfOneMethod_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfPad(TypType typType) => new LfPad((lfPad*) ((byte*) typType.value + 2));
        public static implicit operator LfPointer(TypType typType) => new LfPointer((lfPointer*) ((byte*) typType.value + 2));
        public static implicit operator LfPointer16t(TypType typType) => new LfPointer16t((lfPointer_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfPreComp(TypType typType) => new LfPreComp((lfPreComp*) ((byte*) typType.value + 2));
        public static implicit operator LfPreComp16t(TypType typType) => new LfPreComp16t((lfPreComp_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfProc(TypType typType) => new LfProc((lfProc*) ((byte*) typType.value + 2));
        public static implicit operator LfProc16t(TypType typType) => new LfProc16t((lfProc_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfQuad(TypType typType) => new LfQuad((lfQuad*) ((byte*) typType.value + 2));
        public static implicit operator LfReal128(TypType typType) => new LfReal128((lfReal128*) ((byte*) typType.value + 2));
        public static implicit operator LfReal16(TypType typType) => new LfReal16((lfReal16*) ((byte*) typType.value + 2));
        public static implicit operator LfReal32(TypType typType) => new LfReal32((lfReal32*) ((byte*) typType.value + 2));
        public static implicit operator LfReal48(TypType typType) => new LfReal48((lfReal48*) ((byte*) typType.value + 2));
        public static implicit operator LfReal64(TypType typType) => new LfReal64((lfReal64*) ((byte*) typType.value + 2));
        public static implicit operator LfReal80(TypType typType) => new LfReal80((lfReal80*) ((byte*) typType.value + 2));
        public static implicit operator LfRefSym(TypType typType) => new LfRefSym((lfRefSym*) ((byte*) typType.value + 2));
        public static implicit operator LfShort(TypType typType) => new LfShort((lfShort*) ((byte*) typType.value + 2));
        public static implicit operator LfSkip(TypType typType) => new LfSkip((lfSkip*) ((byte*) typType.value + 2));
        public static implicit operator LfSkip16t(TypType typType) => new LfSkip16t((lfSkip_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfSTMember(TypType typType) => new LfSTMember((lfSTMember*) ((byte*) typType.value + 2));
        public static implicit operator LfSTMember16t(TypType typType) => new LfSTMember16t((lfSTMember_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfStridedArray(TypType typType) => new LfStridedArray((lfStridedArray*) ((byte*) typType.value + 2));
        public static implicit operator LfStringId(TypType typType) => new LfStringId((lfStringId*) ((byte*) typType.value + 2));
        public static implicit operator LfTypeServer(TypType typType) => new LfTypeServer((lfTypeServer*) ((byte*) typType.value + 2));
        public static implicit operator LfTypeServer2(TypType typType) => new LfTypeServer2((lfTypeServer2*) ((byte*) typType.value + 2));
        public static implicit operator LfUdtModSrcLine(TypType typType) => new LfUdtModSrcLine((lfUdtModSrcLine*) ((byte*) typType.value + 2));
        public static implicit operator LfUdtSrcLine(TypType typType) => new LfUdtSrcLine((lfUdtSrcLine*) ((byte*) typType.value + 2));
        public static implicit operator LfULong(TypType typType) => new LfULong((lfULong*) ((byte*) typType.value + 2));
        public static implicit operator LfUnion(TypType typType) => new LfUnion((lfUnion*) ((byte*) typType.value + 2));
        public static implicit operator LfUnion16t(TypType typType) => new LfUnion16t((lfUnion_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfUOct(TypType typType) => new LfUOct((lfUOct*) ((byte*) typType.value + 2));
        public static implicit operator LfUQuad(TypType typType) => new LfUQuad((lfUQuad*) ((byte*) typType.value + 2));
        public static implicit operator LfUShort(TypType typType) => new LfUShort((lfUShort*) ((byte*) typType.value + 2));
        public static implicit operator LfVarString(TypType typType) => new LfVarString((lfVarString*) ((byte*) typType.value + 2));
        public static implicit operator LfVBClass(TypType typType) => new LfVBClass((lfVBClass*) ((byte*) typType.value + 2));
        public static implicit operator LfVBClass16t(TypType typType) => new LfVBClass16t((lfVBClass_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfVector(TypType typType) => new LfVector((lfVector*) ((byte*) typType.value + 2));
        public static implicit operator LfVftable(TypType typType) => new LfVftable((lfVftable*) ((byte*) typType.value + 2));
        public static implicit operator LfVFTPath(TypType typType) => new LfVFTPath((lfVFTPath*) ((byte*) typType.value + 2));
        public static implicit operator LfVFTPath16t(TypType typType) => new LfVFTPath16t((lfVFTPath_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfVFuncOff(TypType typType) => new LfVFuncOff((lfVFuncOff*) ((byte*) typType.value + 2));
        public static implicit operator LfVFuncOff16t(TypType typType) => new LfVFuncOff16t((lfVFuncOff_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfVFuncTab(TypType typType) => new LfVFuncTab((lfVFuncTab*) ((byte*) typType.value + 2));
        public static implicit operator LfVFuncTab16t(TypType typType) => new LfVFuncTab16t((lfVFuncTab_16t*) ((byte*) typType.value + 2));
        public static implicit operator LfVTShape(TypType typType) => new LfVTShape((lfVTShape*) ((byte*) typType.value + 2));
    }
}
