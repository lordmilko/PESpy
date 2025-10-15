using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public abstract class TypTypeDispatcher
    {
        public void Dispatch(TypType typType) => Dispatch((LfEasy) typType);

        public void Dispatch(LfEasy easy)
        {
            switch (easy.leaf)
            {
                case LEAF_ENUM_e.LF_MODIFIER_16t:    LfModifier16t((LfModifier16t) easy); break;
                case LEAF_ENUM_e.LF_POINTER_16t:     LfPointer16t((LfPointer16t) easy); break;
                case LEAF_ENUM_e.LF_ARRAY_16t:       LfArray16t((LfArray16t) easy); break;

                case LEAF_ENUM_e.LF_CLASS_16t:
                case LEAF_ENUM_e.LF_STRUCTURE_16t:
                    LfClass16t((LfClass16t) easy);
                    break;

                case LEAF_ENUM_e.LF_UNION_16t:       LfUnion16t((LfUnion16t) easy); break;
                case LEAF_ENUM_e.LF_ENUM_16t:        LfEnum16t((LfEnum16t) easy); break;
                case LEAF_ENUM_e.LF_PROCEDURE_16t:   LfProc16t((LfProc16t) easy); break;
                case LEAF_ENUM_e.LF_MFUNCTION_16t:   LfMFunc16t((LfMFunc16t) easy); break;
                case LEAF_ENUM_e.LF_VTSHAPE:         LfVTShape((LfVTShape) easy); break;
                case LEAF_ENUM_e.LF_COBOL0_16t:      LfCobol016t((LfCobol016t) easy); break;
                case LEAF_ENUM_e.LF_COBOL1:          LfCobol1((LfCobol1) easy); break;
                case LEAF_ENUM_e.LF_BARRAY_16t:      LfBArray16t((LfBArray16t) easy); break;
                case LEAF_ENUM_e.LF_LABEL:           LfLabel((LfLabel) easy); break;

                case LEAF_ENUM_e.LF_NULL:
                case LEAF_ENUM_e.LF_NOTTRAN:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_DIMARRAY_16t:    LfDimArray16t((LfDimArray16t) easy); break;
                case LEAF_ENUM_e.LF_VFTPATH_16t:     LfVFTPath16t((LfVFTPath16t) easy); break;
                case LEAF_ENUM_e.LF_PRECOMP_16t:     LfPreComp16t((LfPreComp16t) easy); break;
                case LEAF_ENUM_e.LF_ENDPRECOMP:      LfEndPreComp((LfEndPreComp) easy); break;
                case LEAF_ENUM_e.LF_OEM_16t:         LfOEM16t((LfOEM16t) easy); break;
                case LEAF_ENUM_e.LF_SKIP_16t:        LfSkip16t((LfSkip16t) easy); break;
                case LEAF_ENUM_e.LF_ARGLIST_16t:     LfArgList16t((LfArgList16t) easy); break;
                case LEAF_ENUM_e.LF_DEFARG_16t:      LfDefArg16t((LfDefArg16t) easy); break;
                case LEAF_ENUM_e.LF_LIST:            LfList((LfList) easy); break;
                case LEAF_ENUM_e.LF_FIELDLIST_16t:   LfFieldList16t((LfFieldList16t) easy); break;
                case LEAF_ENUM_e.LF_DERIVED_16t:     LfDerived16t((LfDerived16t) easy); break;
                case LEAF_ENUM_e.LF_BITFIELD_16t:    LfBitfield16t((LfBitfield16t) easy); break;
                case LEAF_ENUM_e.LF_METHODLIST_16t:  LfMethodList16t((LfMethodList16t) easy); break;

                case LEAF_ENUM_e.LF_DIMCONU_16t:
                case LEAF_ENUM_e.LF_DIMCONLU_16t:
                    LfDimCon16t((LfDimCon16t) easy);
                    break;

                case LEAF_ENUM_e.LF_DIMVARU_16t:
                case LEAF_ENUM_e.LF_DIMVARLU_16t:
                    LfDimVar16t((LfDimVar16t) easy);
                    break;

                case LEAF_ENUM_e.LF_REFSYM:      LfRefSym((LfRefSym) easy); break;
                case LEAF_ENUM_e.LF_BCLASS_16t:  LfBClass16t((LfBClass16t) easy); break;

                case LEAF_ENUM_e.LF_VBCLASS_16t:
                case LEAF_ENUM_e.LF_IVBCLASS_16t:
                    LfVBClass16t((LfVBClass16t) easy);
                    break;
                
                case LEAF_ENUM_e.LF_FRIENDFCN_16t:  LfFriendFcn16t((LfFriendFcn16t) easy); break;
                case LEAF_ENUM_e.LF_INDEX_16t:      LfIndex16t((LfIndex16t) easy); break;
                case LEAF_ENUM_e.LF_MEMBER_16t:     LfMember16t((LfMember16t) easy); break;
                case LEAF_ENUM_e.LF_STMEMBER_16t:   LfSTMember16t((LfSTMember16t) easy); break;
                case LEAF_ENUM_e.LF_METHOD_16t:     LfMethod16t((LfMethod16t) easy); break;
                case LEAF_ENUM_e.LF_NESTTYPE_16t:   LfNestType16t((LfNestType16t) easy); break;
                case LEAF_ENUM_e.LF_VFUNCTAB_16t:   LfVFuncTab16t((LfVFuncTab16t) easy); break;
                case LEAF_ENUM_e.LF_FRIENDCLS_16t:  LfFriendCls16t((LfFriendCls16t) easy); break;
                case LEAF_ENUM_e.LF_ONEMETHOD_16t:  LfOneMethod16t((LfOneMethod16t) easy); break;
                case LEAF_ENUM_e.LF_VFUNCOFF_16t:   LfVFuncOff16t((LfVFuncOff16t) easy); break;
                case LEAF_ENUM_e.LF_MODIFIER:       LfModifier((LfModifier) easy); break;
                case LEAF_ENUM_e.LF_POINTER:        LfPointer((LfPointer) easy); break;

                case LEAF_ENUM_e.LF_ARRAY_ST:
                case LEAF_ENUM_e.LF_ARRAY:
                    LfArray((LfArray) easy);
                    break;

                case LEAF_ENUM_e.LF_CLASS_ST:
                case LEAF_ENUM_e.LF_STRUCTURE_ST:
                case LEAF_ENUM_e.LF_CLASS:
                case LEAF_ENUM_e.LF_STRUCTURE:
                case LEAF_ENUM_e.LF_INTERFACE:
                    LfClass((LfClass) easy);
                    break;

                case LEAF_ENUM_e.LF_UNION_ST:
                case LEAF_ENUM_e.LF_UNION:
                    LfUnion((LfUnion) easy);
                    break;

                case LEAF_ENUM_e.LF_ENUM_ST:
                case LEAF_ENUM_e.LF_ENUM:
                    LfEnum((LfEnum) easy);
                    break;

                case LEAF_ENUM_e.LF_PROCEDURE:    LfProc((LfProc) easy); break;
                case LEAF_ENUM_e.LF_MFUNCTION:    LfMFunc((LfMFunc) easy); break;
                case LEAF_ENUM_e.LF_COBOL0:       LfCobol0((LfCobol0) easy); break;
                case LEAF_ENUM_e.LF_BARRAY:       LfBArray((LfBArray) easy); break;
                case LEAF_ENUM_e.LF_VFTPATH:      LfVFTPath((LfVFTPath) easy); break;

                case LEAF_ENUM_e.LF_OEM:
                case LEAF_ENUM_e.LF_OEM2:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_SKIP:       LfSkip((LfSkip) easy); break;

                case LEAF_ENUM_e.LF_ARGLIST:
                case LEAF_ENUM_e.LF_SUBSTR_LIST:
                    LfArgList((LfArgList) easy);
                    break;

                case LEAF_ENUM_e.LF_FIELDLIST:   LfFieldList((LfFieldList) easy); break;
                case LEAF_ENUM_e.LF_DERIVED:     LfDerived((LfDerived) easy); break;
                case LEAF_ENUM_e.LF_BITFIELD:    LfBitfield((LfBitfield) easy); break;
                case LEAF_ENUM_e.LF_METHODLIST:  LfMethodList((LfMethodList) easy); break;

                case LEAF_ENUM_e.LF_DIMCONU:
                case LEAF_ENUM_e.LF_DIMCONLU:
                    LfDimCon((LfDimCon) easy);
                    break;

                case LEAF_ENUM_e.LF_DIMVARU:
                case LEAF_ENUM_e.LF_DIMVARLU:
                    LfDimVar((LfDimVar) easy);
                    break;

                case LEAF_ENUM_e.LF_BCLASS:
                case LEAF_ENUM_e.LF_BINTERFACE:
                    LfBClass((LfBClass) easy);
                    break;

                case LEAF_ENUM_e.LF_VBCLASS:
                    LfVBClass((LfVBClass) easy);
                    break;

                case LEAF_ENUM_e.LF_IVBCLASS:
                    throw new NotImplementedException(); //This type is only ever referenced from other records and so does not have a TYPTYPE.len

                case LEAF_ENUM_e.LF_INDEX:  LfIndex((LfIndex) easy); break;

                case LEAF_ENUM_e.LF_VFUNCTAB:
                    LfVFuncTab((LfVFuncTab) easy);
                    break;

                case LEAF_ENUM_e.LF_FRIENDCLS:
                    LfFriendCls((LfFriendCls) easy);
                    break;

                case LEAF_ENUM_e.LF_VFUNCOFF:
                    LfVFuncOff((LfVFuncOff) easy);
                    break;

                case LEAF_ENUM_e.LF_TYPESERVER:
                case LEAF_ENUM_e.LF_TYPESERVER_ST:
                    LfTypeServer((LfTypeServer) easy);
                    break;

                case LEAF_ENUM_e.LF_ENUMERATE:
                case LEAF_ENUM_e.LF_ENUMERATE_ST:
                    LfEnumerate((LfEnumerate) easy);
                    break;

                case LEAF_ENUM_e.LF_DIMARRAY:
                case LEAF_ENUM_e.LF_DIMARRAY_ST:
                    LfDimArray((LfDimArray) easy);
                    break;

                case LEAF_ENUM_e.LF_PRECOMP:
                case LEAF_ENUM_e.LF_PRECOMP_ST:
                    LfPreComp((LfPreComp) easy);
                    break;

                case LEAF_ENUM_e.LF_ALIAS:
                case LEAF_ENUM_e.LF_ALIAS_ST:
                    LfAlias((LfAlias) easy);
                    break;

                case LEAF_ENUM_e.LF_DEFARG:
                case LEAF_ENUM_e.LF_DEFARG_ST:
                    LfDefArg((LfDefArg) easy);
                    break;

                case LEAF_ENUM_e.LF_FRIENDFCN:
                case LEAF_ENUM_e.LF_FRIENDFCN_ST:
                    LfFriendFcn((LfFriendFcn) easy);
                    break;

                case LEAF_ENUM_e.LF_MEMBER:
                case LEAF_ENUM_e.LF_MEMBER_ST:
                    LfMember((LfMember) easy);
                    break;

                case LEAF_ENUM_e.LF_STMEMBER:
                case LEAF_ENUM_e.LF_STMEMBER_ST:
                    LfSTMember((LfSTMember) easy);
                    break;

                case LEAF_ENUM_e.LF_METHOD:
                case LEAF_ENUM_e.LF_METHOD_ST:
                    LfMethod((LfMethod) easy);
                    break;

                case LEAF_ENUM_e.LF_NESTTYPE:
                case LEAF_ENUM_e.LF_NESTTYPE_ST:
                    LfNestType((LfNestType) easy);
                    break;

                case LEAF_ENUM_e.LF_ONEMETHOD:
                case LEAF_ENUM_e.LF_ONEMETHOD_ST:
                    LfOneMethod((LfOneMethod) easy);
                    break;

                case LEAF_ENUM_e.LF_NESTTYPEEX:
                case LEAF_ENUM_e.LF_NESTTYPEEX_ST:
                    LfNestTypeEx((LfNestTypeEx) easy);
                    break;

                case LEAF_ENUM_e.LF_MEMBERMODIFY:
                case LEAF_ENUM_e.LF_MEMBERMODIFY_ST:
                    LfMemberModify((LfMemberModify) easy);
                    break;

                case LEAF_ENUM_e.LF_MANAGED:
                case LEAF_ENUM_e.LF_MANAGED_ST:
                    LfManaged((LfManaged) easy);
                    break;

                case LEAF_ENUM_e.LF_TYPESERVER2:       LfTypeServer2((LfTypeServer2) easy); break;
                case LEAF_ENUM_e.LF_STRIDED_ARRAY:    throw new NotImplementedException();
                case LEAF_ENUM_e.LF_HLSL:              LfHLSL((LfHLSL) easy); break;
                case LEAF_ENUM_e.LF_MODIFIER_EX:       LfModifierEx((LfModifierEx) easy); break;
                case LEAF_ENUM_e.LF_VECTOR:            LfVector((LfVector) easy); break;
                case LEAF_ENUM_e.LF_MATRIX:            LfMatrix((LfMatrix) easy); break;
                case LEAF_ENUM_e.LF_VFTABLE:           LfVftable((LfVftable) easy); break;
                case LEAF_ENUM_e.LF_FUNC_ID:           LfFuncId((LfFuncId) easy); break;
                case LEAF_ENUM_e.LF_MFUNC_ID:          LfMFuncId((LfMFuncId) easy); break;
                case LEAF_ENUM_e.LF_BUILDINFO:         LfBuildInfo((LfBuildInfo) easy); break;
                case LEAF_ENUM_e.LF_STRING_ID:         LfStringId((LfStringId) easy); break;
                case LEAF_ENUM_e.LF_UDT_SRC_LINE:      LfUdtSrcLine((LfUdtSrcLine) easy); break;
                case LEAF_ENUM_e.LF_UDT_MOD_SRC_LINE:  LfUdtModSrcLine((LfUdtModSrcLine) easy); break;
                case LEAF_ENUM_e.LF_CHAR:              LfChar((LfChar) easy); break;
                case LEAF_ENUM_e.LF_SHORT:             LfShort((LfShort) easy); break;
                case LEAF_ENUM_e.LF_USHORT:            LfUShort((LfUShort) easy); break;
                case LEAF_ENUM_e.LF_LONG:              LfLong((LfLong) easy); break;
                case LEAF_ENUM_e.LF_ULONG:             LfULong((LfULong) easy); break;
                case LEAF_ENUM_e.LF_REAL32:            LfReal32((LfReal32) easy); break;
                case LEAF_ENUM_e.LF_REAL64:            LfReal64((LfReal64) easy); break;
                case LEAF_ENUM_e.LF_REAL80:            LfReal80((LfReal80) easy); break;
                case LEAF_ENUM_e.LF_REAL128:           LfReal128((LfReal128) easy); break;

                case LEAF_ENUM_e.LF_QUADWORD:
                case LEAF_ENUM_e.LF_UQUADWORD:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_REAL48:            LfReal48((LfReal48) easy); break;
                case LEAF_ENUM_e.LF_COMPLEX32:         LfCmplx32((LfCmplx32) easy); break;
                case LEAF_ENUM_e.LF_COMPLEX64:         LfCmplx64((LfCmplx64) easy); break;
                case LEAF_ENUM_e.LF_COMPLEX80:         LfCmplx80((LfCmplx80) easy); break;
                case LEAF_ENUM_e.LF_COMPLEX128:        LfCmplx128((LfCmplx128) easy); break;
                case LEAF_ENUM_e.LF_VARSTRING:         LfVarString((LfVarString) easy); break;

                case LEAF_ENUM_e.LF_OCTWORD:
                case LEAF_ENUM_e.LF_UOCTWORD:
                case LEAF_ENUM_e.LF_DECIMAL:
                case LEAF_ENUM_e.LF_DATE:
                case LEAF_ENUM_e.LF_UTF8STRING:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_REAL16:
                    LfReal16((LfReal16) easy);
                    break;

                case LEAF_ENUM_e.LF_PAD0:
                case LEAF_ENUM_e.LF_PAD1:
                case LEAF_ENUM_e.LF_PAD2:
                case LEAF_ENUM_e.LF_PAD3:
                case LEAF_ENUM_e.LF_PAD4:
                case LEAF_ENUM_e.LF_PAD5:
                case LEAF_ENUM_e.LF_PAD6:
                case LEAF_ENUM_e.LF_PAD7:
                case LEAF_ENUM_e.LF_PAD8:
                case LEAF_ENUM_e.LF_PAD9:
                case LEAF_ENUM_e.LF_PAD10:
                case LEAF_ENUM_e.LF_PAD11:
                case LEAF_ENUM_e.LF_PAD12:
                case LEAF_ENUM_e.LF_PAD13:
                case LEAF_ENUM_e.LF_PAD14:
                case LEAF_ENUM_e.LF_PAD15:
                    throw new NotImplementedException();

                default:
                    Debug.Assert(false);
                    LfEasy(easy);
                    break;
            }
        }

        protected abstract void LfEasy(LfEasy value);

        protected abstract void LfModifier16t(LfModifier16t value);
        protected abstract void LfPointer16t(LfPointer16t value);
        protected abstract void LfArray16t(LfArray16t value);
        protected abstract void LfClass16t(LfClass16t value);
        protected abstract void LfUnion16t(LfUnion16t value);
        protected abstract void LfEnum16t(LfEnum16t value);
        protected abstract void LfProc16t(LfProc16t value);
        protected abstract void LfMFunc16t(LfMFunc16t value);
        protected abstract void LfVTShape(LfVTShape value);
        protected abstract void LfCobol016t(LfCobol016t value);
        protected abstract void LfCobol1(LfCobol1 value);
        protected abstract void LfBArray16t(LfBArray16t value);
        protected abstract void LfLabel(LfLabel value);
        protected abstract void LfDimArray16t(LfDimArray16t value);
        protected abstract void LfVFTPath16t(LfVFTPath16t value);
        protected abstract void LfPreComp16t(LfPreComp16t value);
        protected abstract void LfEndPreComp(LfEndPreComp value);
        protected abstract void LfOEM16t(LfOEM16t value);
        protected abstract void LfSkip16t(LfSkip16t value);
        protected abstract void LfArgList16t(LfArgList16t value);
        protected abstract void LfDefArg16t(LfDefArg16t value);
        protected abstract void LfList(LfList value);
        protected abstract void LfFieldList16t(LfFieldList16t value);
        protected abstract void LfDerived16t(LfDerived16t value);
        protected abstract void LfBitfield16t(LfBitfield16t value);
        protected abstract void LfMethodList16t(LfMethodList16t value);
        protected abstract void LfDimCon16t(LfDimCon16t value);
        protected abstract void LfDimVar16t(LfDimVar16t value);
        protected abstract void LfRefSym(LfRefSym value);
        protected abstract void LfBClass16t(LfBClass16t value);
        protected abstract void LfVBClass16t(LfVBClass16t value);
        protected abstract void LfFriendFcn16t(LfFriendFcn16t value);
        protected abstract void LfIndex16t(LfIndex16t value);
        protected abstract void LfMember16t(LfMember16t value);
        protected abstract void LfSTMember16t(LfSTMember16t value);
        protected abstract void LfMethod16t(LfMethod16t value);
        protected abstract void LfNestType16t(LfNestType16t value);
        protected abstract void LfVFuncTab16t(LfVFuncTab16t value);
        protected abstract void LfFriendCls16t(LfFriendCls16t value);
        protected abstract void LfOneMethod16t(LfOneMethod16t value);
        protected abstract void LfVFuncOff16t(LfVFuncOff16t value);
        protected abstract void LfModifier(LfModifier value);
        protected abstract void LfPointer(LfPointer value);
        protected abstract void LfArray(LfArray value);
        protected abstract void LfClass(LfClass value);
        protected abstract void LfUnion(LfUnion value);
        protected abstract void LfEnum(LfEnum value);
        protected abstract void LfProc(LfProc value);
        protected abstract void LfMFunc(LfMFunc value);
        protected abstract void LfCobol0(LfCobol0 value);
        protected abstract void LfBArray(LfBArray value);
        protected abstract void LfVFTPath(LfVFTPath value);
        protected abstract void LfSkip(LfSkip value);
        protected abstract void LfArgList(LfArgList value);
        protected abstract void LfFieldList(LfFieldList value);
        protected abstract void LfDerived(LfDerived value);
        protected abstract void LfBitfield(LfBitfield value);
        protected abstract void LfMethodList(LfMethodList value);
        protected abstract void LfDimCon(LfDimCon value);
        protected abstract void LfDimVar(LfDimVar value);
        protected abstract void LfBClass(LfBClass value);
        protected abstract void LfVBClass(LfVBClass value);
        protected abstract void LfIndex(LfIndex value);
        protected abstract void LfVFuncTab(LfVFuncTab value);
        protected abstract void LfFriendCls(LfFriendCls value);
        protected abstract void LfVFuncOff(LfVFuncOff value);
        protected abstract void LfTypeServer(LfTypeServer value);
        protected abstract void LfEnumerate(LfEnumerate value);
        protected abstract void LfDimArray(LfDimArray value);
        protected abstract void LfPreComp(LfPreComp value);
        protected abstract void LfAlias(LfAlias value);
        protected abstract void LfDefArg(LfDefArg value);
        protected abstract void LfFriendFcn(LfFriendFcn value);
        protected abstract void LfMember(LfMember value);
        protected abstract void LfSTMember(LfSTMember value);
        protected abstract void LfMethod(LfMethod value);
        protected abstract void LfNestType(LfNestType value);
        protected abstract void LfOneMethod(LfOneMethod value);
        protected abstract void LfNestTypeEx(LfNestTypeEx value);
        protected abstract void LfMemberModify(LfMemberModify value);
        protected abstract void LfManaged(LfManaged value);
        protected abstract void LfTypeServer2(LfTypeServer2 value);
        protected abstract void LfHLSL(LfHLSL value);
        protected abstract void LfModifierEx(LfModifierEx value);
        protected abstract void LfVector(LfVector value);
        protected abstract void LfMatrix(LfMatrix value);
        protected abstract void LfVftable(LfVftable value);
        protected abstract void LfFuncId(LfFuncId value);
        protected abstract void LfMFuncId(LfMFuncId value);
        protected abstract void LfBuildInfo(LfBuildInfo value);
        protected abstract void LfStringId(LfStringId value);
        protected abstract void LfUdtSrcLine(LfUdtSrcLine value);
        protected abstract void LfUdtModSrcLine(LfUdtModSrcLine value);
        protected abstract void LfChar(LfChar value);
        protected abstract void LfShort(LfShort value);
        protected abstract void LfUShort(LfUShort value);
        protected abstract void LfLong(LfLong value);
        protected abstract void LfULong(LfULong value);
        protected abstract void LfReal32(LfReal32 value);
        protected abstract void LfReal64(LfReal64 value);
        protected abstract void LfReal80(LfReal80 value);
        protected abstract void LfReal128(LfReal128 value);
        protected abstract void LfReal48(LfReal48 value);
        protected abstract void LfCmplx32(LfCmplx32 value);
        protected abstract void LfCmplx64(LfCmplx64 value);
        protected abstract void LfCmplx80(LfCmplx80 value);
        protected abstract void LfCmplx128(LfCmplx128 value);
        protected abstract void LfVarString(LfVarString value);
        protected abstract void LfReal16(LfReal16 value);
    }
}
