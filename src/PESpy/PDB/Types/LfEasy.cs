using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEasy"/> structure.
    /// </summary>
    [DebuggerTypeProxy(typeof(TypTypeProxy))]
    [DebuggerDisplay("{TypTypeProxy.DebuggerDisplay(this),nq}")]
    public readonly unsafe struct LfEasy : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEasy* value;

        public ushort typlen
        {
            get
            {
                //Based on my reading of cvinfo.h, leaves >= 0x200 but less than 0x1000 and >= 0x1200 and < 1500 are only referenced from other type records,
                //and therefore don't have lengths. However, this is is not correct. Some of these items do nicely fit into ranges, but other's (such as LF_MEMBER) don't.
                //On the one hand, you could say that the safest thing to do for these types is to just return 0 to indicate they're not a real TYPTYPE*, but given you can't
                //even get to these without digging into a field list, and I want to have a way to get at the lengths of these, we _will_ lookup the length of each type and
                //return it to the caller
                switch (value->leaf)
                {
                    case LEAF_ENUM_e.LF_BCLASS_16t:
                        return (ushort) ((LfBClass16t) this).StructSize;

                    case LEAF_ENUM_e.LF_BCLASS:
                        return (ushort) ((LfBClass) this).StructSize;

                    case LEAF_ENUM_e.LF_ENUMERATE:
                    case LEAF_ENUM_e.LF_ENUMERATE_ST:
                        return (ushort) ((LfEnumerate) this).StructSize;

                    case LEAF_ENUM_e.LF_FRIENDCLS_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_FRIENDCLS:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_FRIENDFCN_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_FRIENDFCN:
                    case LEAF_ENUM_e.LF_FRIENDFCN_ST:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_INDEX_16t:
                        return LfIndex16t.StructSize;

                    case LEAF_ENUM_e.LF_INDEX:
                        return LfIndex.StructSize;

                    case LEAF_ENUM_e.LF_IVBCLASS_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_IVBCLASS:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_MEMBER_16t:
                        return (ushort) ((LfMember16t) this).StructSize;

                    case LEAF_ENUM_e.LF_MEMBER:
                    case LEAF_ENUM_e.LF_MEMBER_ST:
                        return (ushort) ((LfMember) this).StructSize;

                    case LEAF_ENUM_e.LF_METHOD_16t:
                        return (ushort) ((LfMethod16t) this).StructSize;

                    case LEAF_ENUM_e.LF_METHOD:
                    case LEAF_ENUM_e.LF_METHOD_ST:
                        return (ushort) ((LfMethod) this).StructSize;

                    case LEAF_ENUM_e.LF_NESTTYPE_16t:
                        return (ushort) ((LfNestType16t) this).StructSize;

                    case LEAF_ENUM_e.LF_NESTTYPE:
                    case LEAF_ENUM_e.LF_NESTTYPE_ST:
                        return (ushort) ((LfNestType) this).StructSize;

                    case LEAF_ENUM_e.LF_ONEMETHOD_16t:
                        return (ushort) ((LfOneMethod16t) this).StructSize;

                    case LEAF_ENUM_e.LF_ONEMETHOD:
                    case LEAF_ENUM_e.LF_ONEMETHOD_ST:
                        return (ushort) ((LfOneMethod) this).StructSize;

                    case LEAF_ENUM_e.LF_STMEMBER_16t:
                        return (ushort) ((LfSTMember16t) this).StructSize;

                    case LEAF_ENUM_e.LF_STMEMBER:
                    case LEAF_ENUM_e.LF_STMEMBER_ST:
                        return (ushort) ((LfSTMember) this).StructSize;

                    case LEAF_ENUM_e.LF_VBCLASS_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_VBCLASS:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_VFUNCTAB_16t:
                        return LfVFuncTab16t.StructSize;

                    case LEAF_ENUM_e.LF_VFUNCTAB:
                        return LfVFuncTab.StructSize;

                    default:
                        return *(ushort*) ((byte*) value - 2);
                }
            }
        }

        public LEAF_ENUM_e leaf => value->leaf;

        internal const int StructSize =
            sizeof(ushort);  //leaf

        internal LfEasy(lfEasy* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEasy, this, ViewKind.LfEasy, typlen + sizeof(short));

        int IViewable.NumChildren() => typlen > 2 ? 3 : 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Unlike with SymType, we should not be writing an unknown type as LfEasy
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 2:
                    //Our dispatcher will dispatch to LfEasy in the event of an unsupported top level type.
                    //LfFieldList will crash in the event of an unsupported type, so we should never have a 0 typlen
                    Debug.Assert(typlen != 0);
                    structWriter.WriteByteBlob(4, typlen - sizeof(ushort));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            if (value == default)
                return "<null>";

            return StringTypTypeDispatcher.Instance.Dispatch(this);
        }

        public static implicit operator LfEasy(lfEasy* value) => new LfEasy(value);
        public static implicit operator lfEasy*(LfEasy value) => value.value;

        public static implicit operator TypType(LfEasy value) => new TypType((TYPTYPE*) ((byte*) value.value - 2));

        //todo: need reverse casts. do they go here or in each type's file directly?

        public static implicit operator LfAlias(LfEasy easy) => new LfAlias((lfAlias*) (byte*) easy.value);
        public static implicit operator LfArgList(LfEasy easy) => new LfArgList((lfArgList*) (byte*) easy.value);
        public static implicit operator LfArgList16t(LfEasy easy) => new LfArgList16t((lfArgList_16t*) (byte*) easy.value);
        public static implicit operator LfArray(LfEasy easy) => new LfArray((lfArray*) (byte*) easy.value);
        public static implicit operator LfArray16t(LfEasy easy) => new LfArray16t((lfArray_16t*) (byte*) easy.value);
        public static implicit operator LfBArray(LfEasy easy) => new LfBArray((lfBArray*) (byte*) easy.value);
        public static implicit operator LfBArray16t(LfEasy easy) => new LfBArray16t((lfBArray_16t*) (byte*) easy.value);
        public static implicit operator LfBClass(LfEasy easy) => new LfBClass((lfBClass*) (byte*) easy.value);
        public static implicit operator LfBClass16t(LfEasy easy) => new LfBClass16t((lfBClass_16t*) (byte*) easy.value);
        public static implicit operator LfBitfield(LfEasy easy) => new LfBitfield((lfBitfield*) (byte*) easy.value);
        public static implicit operator LfBitfield16t(LfEasy easy) => new LfBitfield16t((lfBitfield_16t*) (byte*) easy.value);
        public static implicit operator LfBuildInfo(LfEasy easy) => new LfBuildInfo((lfBuildInfo*) (byte*) easy.value);
        public static implicit operator LfChar(LfEasy easy) => new LfChar((lfChar*) (byte*) easy.value);
        public static implicit operator LfClass(LfEasy easy) => new LfClass((lfClass*) (byte*) easy.value);
        public static implicit operator LfClass16t(LfEasy easy) => new LfClass16t((lfClass_16t*) (byte*) easy.value);
        public static implicit operator LfCmplx128(LfEasy easy) => new LfCmplx128((lfCmplx128*) (byte*) easy.value);
        public static implicit operator LfCmplx32(LfEasy easy) => new LfCmplx32((lfCmplx32*) (byte*) easy.value);
        public static implicit operator LfCmplx64(LfEasy easy) => new LfCmplx64((lfCmplx64*) (byte*) easy.value);
        public static implicit operator LfCmplx80(LfEasy easy) => new LfCmplx80((lfCmplx80*) (byte*) easy.value);
        public static implicit operator LfCobol0(LfEasy easy) => new LfCobol0((lfCobol0*) (byte*) easy.value);
        public static implicit operator LfCobol016t(LfEasy easy) => new LfCobol016t((lfCobol0_16t*) (byte*) easy.value);
        public static implicit operator LfCobol1(LfEasy easy) => new LfCobol1((lfCobol1*) (byte*) easy.value);
        public static implicit operator LfDefArg(LfEasy easy) => new LfDefArg((lfDefArg*) (byte*) easy.value);
        public static implicit operator LfDefArg16t(LfEasy easy) => new LfDefArg16t((lfDefArg_16t*) (byte*) easy.value);
        public static implicit operator LfDerived(LfEasy easy) => new LfDerived((lfDerived*) (byte*) easy.value);
        public static implicit operator LfDerived16t(LfEasy easy) => new LfDerived16t((lfDerived_16t*) (byte*) easy.value);
        public static implicit operator LfDimArray(LfEasy easy) => new LfDimArray((lfDimArray*) (byte*) easy.value);
        public static implicit operator LfDimArray16t(LfEasy easy) => new LfDimArray16t((lfDimArray_16t*) (byte*) easy.value);
        public static implicit operator LfDimCon(LfEasy easy) => new LfDimCon((lfDimCon*) (byte*) easy.value);
        public static implicit operator LfDimCon16t(LfEasy easy) => new LfDimCon16t((lfDimCon_16t*) (byte*) easy.value);
        public static implicit operator LfDimVar(LfEasy easy) => new LfDimVar((lfDimVar*) (byte*) easy.value);
        public static implicit operator LfDimVar16t(LfEasy easy) => new LfDimVar16t((lfDimVar_16t*) (byte*) easy.value);
        public static implicit operator LfEndPreComp(LfEasy easy) => new LfEndPreComp((lfEndPreComp*) (byte*) easy.value);
        public static implicit operator LfEnum(LfEasy easy) => new LfEnum((lfEnum*) (byte*) easy.value);
        public static implicit operator LfEnum16t(LfEasy easy) => new LfEnum16t((lfEnum_16t*) (byte*) easy.value);
        public static implicit operator LfEnumerate(LfEasy easy) => new LfEnumerate((lfEnumerate*) (byte*) easy.value);
        public static implicit operator LfFieldList(LfEasy easy) => new LfFieldList((lfFieldList*) (byte*) easy.value);
        public static implicit operator LfFieldList16t(LfEasy easy) => new LfFieldList16t((lfFieldList_16t*) (byte*) easy.value);
        public static implicit operator LfFriendCls(LfEasy easy) => new LfFriendCls((lfFriendCls*) (byte*) easy.value);
        public static implicit operator LfFriendCls16t(LfEasy easy) => new LfFriendCls16t((lfFriendCls_16t*) (byte*) easy.value);
        public static implicit operator LfFriendFcn(LfEasy easy) => new LfFriendFcn((lfFriendFcn*) (byte*) easy.value);
        public static implicit operator LfFriendFcn16t(LfEasy easy) => new LfFriendFcn16t((lfFriendFcn_16t*) (byte*) easy.value);
        public static implicit operator LfFuncId(LfEasy easy) => new LfFuncId((lfFuncId*) (byte*) easy.value);
        public static implicit operator LfHLSL(LfEasy easy) => new LfHLSL((lfHLSL*) (byte*) easy.value);
        public static implicit operator LfIndex(LfEasy easy) => new LfIndex((lfIndex*) (byte*) easy.value);
        public static implicit operator LfIndex16t(LfEasy easy) => new LfIndex16t((lfIndex_16t*) (byte*) easy.value);
        public static implicit operator LfLabel(LfEasy easy) => new LfLabel((lfLabel*) (byte*) easy.value);
        public static implicit operator LfList(LfEasy easy) => new LfList((lfList*) (byte*) easy.value);
        public static implicit operator LfLong(LfEasy easy) => new LfLong((lfLong*) (byte*) easy.value);
        public static implicit operator LfManaged(LfEasy easy) => new LfManaged((lfManaged*) (byte*) easy.value);
        public static implicit operator LfMatrix(LfEasy easy) => new LfMatrix((lfMatrix*) (byte*) easy.value);
        public static implicit operator LfMember(LfEasy easy) => new LfMember((lfMember*) (byte*) easy.value);
        public static implicit operator LfMember16t(LfEasy easy) => new LfMember16t((lfMember_16t*) (byte*) easy.value);
        public static implicit operator LfMemberModify(LfEasy easy) => new LfMemberModify((lfMemberModify*) (byte*) easy.value);
        public static implicit operator LfMethod(LfEasy easy) => new LfMethod((lfMethod*) (byte*) easy.value);
        public static implicit operator LfMethod16t(LfEasy easy) => new LfMethod16t((lfMethod_16t*) (byte*) easy.value);
        public static implicit operator LfMethodList(LfEasy easy) => new LfMethodList((lfMethodList*) (byte*) easy.value);
        public static implicit operator LfMethodList16t(LfEasy easy) => new LfMethodList16t((lfMethodList_16t*) (byte*) easy.value);
        public static implicit operator LfMFunc(LfEasy easy) => new LfMFunc((lfMFunc*) (byte*) easy.value);
        public static implicit operator LfMFunc16t(LfEasy easy) => new LfMFunc16t((lfMFunc_16t*) (byte*) easy.value);
        public static implicit operator LfMFuncId(LfEasy easy) => new LfMFuncId((lfMFuncId*) (byte*) easy.value);
        public static implicit operator LfModifier(LfEasy easy) => new LfModifier((lfModifier*) (byte*) easy.value);
        public static implicit operator LfModifier16t(LfEasy easy) => new LfModifier16t((lfModifier_16t*) (byte*) easy.value);
        public static implicit operator LfModifierEx(LfEasy easy) => new LfModifierEx((lfModifierEx*) (byte*) easy.value);
        public static implicit operator LfNestType(LfEasy easy) => new LfNestType((lfNestType*) (byte*) easy.value);
        public static implicit operator LfNestType16t(LfEasy easy) => new LfNestType16t((lfNestType_16t*) (byte*) easy.value);
        public static implicit operator LfNestTypeEx(LfEasy easy) => new LfNestTypeEx((lfNestTypeEx*) (byte*) easy.value);
        public static implicit operator LfOct(LfEasy easy) => new LfOct((lfOct*) (byte*) easy.value);
        public static implicit operator LfOEM(LfEasy easy) => new LfOEM((lfOEM*) (byte*) easy.value);
        public static implicit operator LfOEM16t(LfEasy easy) => new LfOEM16t((lfOEM_16t*) (byte*) easy.value);
        public static implicit operator LfOEM2(LfEasy easy) => new LfOEM2((lfOEM2*) (byte*) easy.value);
        public static implicit operator LfOneMethod(LfEasy easy) => new LfOneMethod((lfOneMethod*) (byte*) easy.value);
        public static implicit operator LfOneMethod16t(LfEasy easy) => new LfOneMethod16t((lfOneMethod_16t*) (byte*) easy.value);
        public static implicit operator LfPad(LfEasy easy) => new LfPad((lfPad*) (byte*) easy.value);
        public static implicit operator LfPointer(LfEasy easy) => new LfPointer((lfPointer*) (byte*) easy.value);
        public static implicit operator LfPointer16t(LfEasy easy) => new LfPointer16t((lfPointer_16t*) (byte*) easy.value);
        public static implicit operator LfPreComp(LfEasy easy) => new LfPreComp((lfPreComp*) (byte*) easy.value);
        public static implicit operator LfPreComp16t(LfEasy easy) => new LfPreComp16t((lfPreComp_16t*) (byte*) easy.value);
        public static implicit operator LfProc(LfEasy easy) => new LfProc((lfProc*) (byte*) easy.value);
        public static implicit operator LfProc16t(LfEasy easy) => new LfProc16t((lfProc_16t*) (byte*) easy.value);
        public static implicit operator LfQuad(LfEasy easy) => new LfQuad((lfQuad*) (byte*) easy.value);
        public static implicit operator LfReal128(LfEasy easy) => new LfReal128((lfReal128*) (byte*) easy.value);
        public static implicit operator LfReal16(LfEasy easy) => new LfReal16((lfReal16*) (byte*) easy.value);
        public static implicit operator LfReal32(LfEasy easy) => new LfReal32((lfReal32*) (byte*) easy.value);
        public static implicit operator LfReal48(LfEasy easy) => new LfReal48((lfReal48*) (byte*) easy.value);
        public static implicit operator LfReal64(LfEasy easy) => new LfReal64((lfReal64*) (byte*) easy.value);
        public static implicit operator LfReal80(LfEasy easy) => new LfReal80((lfReal80*) (byte*) easy.value);
        public static implicit operator LfRefSym(LfEasy easy) => new LfRefSym((lfRefSym*) (byte*) easy.value);
        public static implicit operator LfShort(LfEasy easy) => new LfShort((lfShort*) (byte*) easy.value);
        public static implicit operator LfSkip(LfEasy easy) => new LfSkip((lfSkip*) (byte*) easy.value);
        public static implicit operator LfSkip16t(LfEasy easy) => new LfSkip16t((lfSkip_16t*) (byte*) easy.value);
        public static implicit operator LfSTMember(LfEasy easy) => new LfSTMember((lfSTMember*) (byte*) easy.value);
        public static implicit operator LfSTMember16t(LfEasy easy) => new LfSTMember16t((lfSTMember_16t*) (byte*) easy.value);
        public static implicit operator LfStridedArray(LfEasy easy) => new LfStridedArray((lfStridedArray*) (byte*) easy.value);
        public static implicit operator LfStringId(LfEasy easy) => new LfStringId((lfStringId*) (byte*) easy.value);
        public static implicit operator LfTypeServer(LfEasy easy) => new LfTypeServer((lfTypeServer*) (byte*) easy.value);
        public static implicit operator LfTypeServer2(LfEasy easy) => new LfTypeServer2((lfTypeServer2*) (byte*) easy.value);
        public static implicit operator LfUdtModSrcLine(LfEasy easy) => new LfUdtModSrcLine((lfUdtModSrcLine*) (byte*) easy.value);
        public static implicit operator LfUdtSrcLine(LfEasy easy) => new LfUdtSrcLine((lfUdtSrcLine*) (byte*) easy.value);
        public static implicit operator LfULong(LfEasy easy) => new LfULong((lfULong*) (byte*) easy.value);
        public static implicit operator LfUnion(LfEasy easy) => new LfUnion((lfUnion*) (byte*) easy.value);
        public static implicit operator LfUnion16t(LfEasy easy) => new LfUnion16t((lfUnion_16t*) (byte*) easy.value);
        public static implicit operator LfUOct(LfEasy easy) => new LfUOct((lfUOct*) (byte*) easy.value);
        public static implicit operator LfUQuad(LfEasy easy) => new LfUQuad((lfUQuad*) (byte*) easy.value);
        public static implicit operator LfUShort(LfEasy easy) => new LfUShort((lfUShort*) (byte*) easy.value);
        public static implicit operator LfVarString(LfEasy easy) => new LfVarString((lfVarString*) (byte*) easy.value);
        public static implicit operator LfVBClass(LfEasy easy) => new LfVBClass((lfVBClass*) (byte*) easy.value);
        public static implicit operator LfVBClass16t(LfEasy easy) => new LfVBClass16t((lfVBClass_16t*) (byte*) easy.value);
        public static implicit operator LfVector(LfEasy easy) => new LfVector((lfVector*) (byte*) easy.value);
        public static implicit operator LfVftable(LfEasy easy) => new LfVftable((lfVftable*) (byte*) easy.value);
        public static implicit operator LfVFTPath(LfEasy easy) => new LfVFTPath((lfVFTPath*) (byte*) easy.value);
        public static implicit operator LfVFTPath16t(LfEasy easy) => new LfVFTPath16t((lfVFTPath_16t*) (byte*) easy.value);
        public static implicit operator LfVFuncOff(LfEasy easy) => new LfVFuncOff((lfVFuncOff*) (byte*) easy.value);
        public static implicit operator LfVFuncOff16t(LfEasy easy) => new LfVFuncOff16t((lfVFuncOff_16t*) (byte*) easy.value);
        public static implicit operator LfVFuncTab(LfEasy easy) => new LfVFuncTab((lfVFuncTab*) (byte*) easy.value);
        public static implicit operator LfVFuncTab16t(LfEasy easy) => new LfVFuncTab16t((lfVFuncTab_16t*) (byte*) easy.value);
        public static implicit operator LfVTShape(LfEasy easy) => new LfVTShape((lfVTShape*) (byte*) easy.value);
    }
}
