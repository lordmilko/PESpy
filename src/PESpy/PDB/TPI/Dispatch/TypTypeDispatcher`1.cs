using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    public abstract class TypTypeDispatcher<T>
    {
        public T Dispatch(TypType typType)
        {
            switch (typType.leaf)
            {
                case LEAF_ENUM_e.LF_MODIFIER_16t:   return LfModifier16t((LfModifier16t) typType);
                case LEAF_ENUM_e.LF_POINTER_16t:    return LfPointer16t((LfPointer16t) typType);
                case LEAF_ENUM_e.LF_ARRAY_16t:      return LfArray16t((LfArray16t) typType);

                case LEAF_ENUM_e.LF_CLASS_16t:
                case LEAF_ENUM_e.LF_STRUCTURE_16t:
                    return LfClass16t((LfClass16t) typType);

                case LEAF_ENUM_e.LF_UNION_16t:      return LfUnion16t((LfUnion16t) typType);
                case LEAF_ENUM_e.LF_ENUM_16t:       return LfEnum16t((LfEnum16t) typType);
                case LEAF_ENUM_e.LF_PROCEDURE_16t:  return LfProc16t((LfProc16t) typType);
                case LEAF_ENUM_e.LF_MFUNCTION_16t:  return LfMFunc16t((LfMFunc16t) typType);
                case LEAF_ENUM_e.LF_VTSHAPE:        return LfVTShape((LfVTShape) typType);
                case LEAF_ENUM_e.LF_COBOL0_16t:     return LfCobol016t((LfCobol016t) typType);
                case LEAF_ENUM_e.LF_COBOL1:         return LfCobol1((LfCobol1) typType);
                case LEAF_ENUM_e.LF_BARRAY_16t:     return LfBArray16t((LfBArray16t) typType);
                case LEAF_ENUM_e.LF_LABEL:          return LfLabel((LfLabel) typType);

                case LEAF_ENUM_e.LF_NULL:
                case LEAF_ENUM_e.LF_NOTTRAN:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_DIMARRAY_16t:   return LfDimArray16t((LfDimArray16t) typType);
                case LEAF_ENUM_e.LF_VFTPATH_16t:    return LfVFTPath16t((LfVFTPath16t) typType);
                case LEAF_ENUM_e.LF_PRECOMP_16t:    return LfPreComp16t((LfPreComp16t) typType);
                case LEAF_ENUM_e.LF_ENDPRECOMP:     return LfEndPreComp((LfEndPreComp) typType);
                case LEAF_ENUM_e.LF_OEM_16t:        return LfOEM16t((LfOEM16t) typType);
                case LEAF_ENUM_e.LF_SKIP_16t:       return LfSkip16t((LfSkip16t) typType);
                case LEAF_ENUM_e.LF_ARGLIST_16t:    return LfArgList16t((LfArgList16t) typType);
                case LEAF_ENUM_e.LF_DEFARG_16t:     return LfDefArg16t((LfDefArg16t) typType);
                case LEAF_ENUM_e.LF_LIST:           return LfList((LfList) typType);
                case LEAF_ENUM_e.LF_FIELDLIST_16t:  return LfFieldList16t((LfFieldList16t) typType);
                case LEAF_ENUM_e.LF_DERIVED_16t:    return LfDerived16t((LfDerived16t) typType);
                case LEAF_ENUM_e.LF_BITFIELD_16t:   return LfBitfield16t((LfBitfield16t) typType);
                case LEAF_ENUM_e.LF_METHODLIST_16t: return LfMethodList16t((LfMethodList16t) typType);

                case LEAF_ENUM_e.LF_DIMCONU_16t:
                case LEAF_ENUM_e.LF_DIMCONLU_16t:
                    return LfDimCon16t((LfDimCon16t) typType);

                case LEAF_ENUM_e.LF_DIMVARU_16t:
                case LEAF_ENUM_e.LF_DIMVARLU_16t:
                    return LfDimVar16t((LfDimVar16t) typType);

                case LEAF_ENUM_e.LF_REFSYM:     return LfRefSym((LfRefSym) typType);
                case LEAF_ENUM_e.LF_BCLASS_16t: return LfBClass16t((LfBClass16t) typType);

                case LEAF_ENUM_e.LF_VBCLASS_16t:
                case LEAF_ENUM_e.LF_IVBCLASS_16t:
                    return LfVBClass16t((LfVBClass16t) typType);
                
                case LEAF_ENUM_e.LF_FRIENDFCN_16t: return LfFriendFcn16t((LfFriendFcn16t) typType);
                case LEAF_ENUM_e.LF_INDEX_16t:     return LfIndex16t((LfIndex16t) typType);
                case LEAF_ENUM_e.LF_MEMBER_16t:    return LfMember16t((LfMember16t) typType);
                case LEAF_ENUM_e.LF_STMEMBER_16t:  return LfSTMember16t((LfSTMember16t) typType);
                case LEAF_ENUM_e.LF_METHOD_16t:    return LfMethod16t((LfMethod16t) typType);
                case LEAF_ENUM_e.LF_NESTTYPE_16t:  return LfNestType16t((LfNestType16t) typType);
                case LEAF_ENUM_e.LF_VFUNCTAB_16t:  return LfVFuncTab16t((LfVFuncTab16t) typType);
                case LEAF_ENUM_e.LF_FRIENDCLS_16t: return LfFriendCls16t((LfFriendCls16t) typType);
                case LEAF_ENUM_e.LF_ONEMETHOD_16t: return LfOneMethod16t((LfOneMethod16t) typType);
                case LEAF_ENUM_e.LF_VFUNCOFF_16t:  return LfVFuncOff16t((LfVFuncOff16t) typType);
                case LEAF_ENUM_e.LF_MODIFIER:      return LfModifier((LfModifier) typType);
                case LEAF_ENUM_e.LF_POINTER:       return LfPointer((LfPointer) typType);

                case LEAF_ENUM_e.LF_ARRAY_ST:
                case LEAF_ENUM_e.LF_ARRAY:
                    return LfArray((LfArray) typType);

                case LEAF_ENUM_e.LF_CLASS_ST:
                case LEAF_ENUM_e.LF_STRUCTURE_ST:
                case LEAF_ENUM_e.LF_CLASS:
                case LEAF_ENUM_e.LF_STRUCTURE:
                case LEAF_ENUM_e.LF_INTERFACE:
                    return LfClass((LfClass) typType);

                case LEAF_ENUM_e.LF_UNION_ST:
                case LEAF_ENUM_e.LF_UNION:
                    return LfUnion((LfUnion) typType);

                case LEAF_ENUM_e.LF_ENUM_ST:
                case LEAF_ENUM_e.LF_ENUM:
                    return LfEnum((LfEnum) typType);

                case LEAF_ENUM_e.LF_PROCEDURE:   return LfProc((LfProc) typType);
                case LEAF_ENUM_e.LF_MFUNCTION:   return LfMFunc((LfMFunc) typType);
                case LEAF_ENUM_e.LF_COBOL0:      return LfCobol0((LfCobol0) typType);
                case LEAF_ENUM_e.LF_BARRAY:      return LfBArray((LfBArray) typType);
                case LEAF_ENUM_e.LF_VFTPATH:     return LfVFTPath((LfVFTPath) typType);

                case LEAF_ENUM_e.LF_OEM:
                case LEAF_ENUM_e.LF_OEM2:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_SKIP:      return LfSkip((LfSkip) typType);

                case LEAF_ENUM_e.LF_ARGLIST:
                case LEAF_ENUM_e.LF_SUBSTR_LIST:
                    return LfArgList((LfArgList) typType);

                case LEAF_ENUM_e.LF_FIELDLIST:  return LfFieldList((LfFieldList) typType);
                case LEAF_ENUM_e.LF_DERIVED:    return LfDerived((LfDerived) typType);
                case LEAF_ENUM_e.LF_BITFIELD:   return LfBitfield((LfBitfield) typType);
                case LEAF_ENUM_e.LF_METHODLIST: return LfMethodList((LfMethodList) typType);

                case LEAF_ENUM_e.LF_DIMCONU:
                case LEAF_ENUM_e.LF_DIMCONLU:
                    return LfDimCon((LfDimCon) typType);

                case LEAF_ENUM_e.LF_DIMVARU:
                case LEAF_ENUM_e.LF_DIMVARLU:
                    return LfDimVar((LfDimVar) typType);

                case LEAF_ENUM_e.LF_BCLASS:
                case LEAF_ENUM_e.LF_BINTERFACE:
                    return LfBClass((LfBClass) typType);

                case LEAF_ENUM_e.LF_VBCLASS:
                    return LfVBClass((LfVBClass) typType);

                case LEAF_ENUM_e.LF_IVBCLASS:
                    throw new NotImplementedException(); //This type is only ever referenced from other records and so does not have a TYPTYPE.len

                case LEAF_ENUM_e.LF_INDEX: return LfIndex((LfIndex) typType);

                case LEAF_ENUM_e.LF_VFUNCTAB:
                    return LfVFuncTab((LfVFuncTab) typType);

                case LEAF_ENUM_e.LF_FRIENDCLS:
                    return LfFriendCls((LfFriendCls) typType);

                case LEAF_ENUM_e.LF_VFUNCOFF:
                    return LfVFuncOff((LfVFuncOff) typType);

                case LEAF_ENUM_e.LF_TYPESERVER:
                case LEAF_ENUM_e.LF_TYPESERVER_ST:
                    return LfTypeServer((LfTypeServer) typType);

                case LEAF_ENUM_e.LF_ENUMERATE:
                case LEAF_ENUM_e.LF_ENUMERATE_ST:
                    return LfEnumerate((LfEnumerate) typType);

                case LEAF_ENUM_e.LF_DIMARRAY:
                case LEAF_ENUM_e.LF_DIMARRAY_ST:
                    return LfDimArray((LfDimArray) typType);

                case LEAF_ENUM_e.LF_PRECOMP:
                case LEAF_ENUM_e.LF_PRECOMP_ST:
                    return LfPreComp((LfPreComp) typType);

                case LEAF_ENUM_e.LF_ALIAS:
                case LEAF_ENUM_e.LF_ALIAS_ST:
                    return LfAlias((LfAlias) typType);

                case LEAF_ENUM_e.LF_DEFARG:
                case LEAF_ENUM_e.LF_DEFARG_ST:
                    return LfDefArg((LfDefArg) typType);

                case LEAF_ENUM_e.LF_FRIENDFCN:
                case LEAF_ENUM_e.LF_FRIENDFCN_ST:
                    return LfFriendFcn((LfFriendFcn) typType);

                case LEAF_ENUM_e.LF_MEMBER:
                case LEAF_ENUM_e.LF_MEMBER_ST:
                    return LfMember((LfMember) typType);

                case LEAF_ENUM_e.LF_STMEMBER:
                case LEAF_ENUM_e.LF_STMEMBER_ST:
                    return LfSTMember((LfSTMember) typType);

                case LEAF_ENUM_e.LF_METHOD:
                case LEAF_ENUM_e.LF_METHOD_ST:
                    return LfMethod((LfMethod) typType);

                case LEAF_ENUM_e.LF_NESTTYPE:
                case LEAF_ENUM_e.LF_NESTTYPE_ST:
                    return LfNestType((LfNestType) typType);

                case LEAF_ENUM_e.LF_ONEMETHOD:
                case LEAF_ENUM_e.LF_ONEMETHOD_ST:
                    return LfOneMethod((LfOneMethod) typType);

                case LEAF_ENUM_e.LF_NESTTYPEEX:
                case LEAF_ENUM_e.LF_NESTTYPEEX_ST:
                    return LfNestTypeEx((LfNestTypeEx) typType);

                case LEAF_ENUM_e.LF_MEMBERMODIFY:
                case LEAF_ENUM_e.LF_MEMBERMODIFY_ST:
                    return LfMemberModify((LfMemberModify) typType);

                case LEAF_ENUM_e.LF_MANAGED:
                case LEAF_ENUM_e.LF_MANAGED_ST:
                    return LfManaged((LfManaged) typType);

                case LEAF_ENUM_e.LF_TYPESERVER2:      return LfTypeServer2((LfTypeServer2) typType);
                case LEAF_ENUM_e.LF_STRIDED_ARRAY:    throw new NotImplementedException();
                case LEAF_ENUM_e.LF_HLSL:             return LfHLSL((LfHLSL) typType);
                case LEAF_ENUM_e.LF_MODIFIER_EX:      return LfModifierEx((LfModifierEx) typType);
                case LEAF_ENUM_e.LF_VECTOR:           return LfVector((LfVector) typType);
                case LEAF_ENUM_e.LF_MATRIX:           return LfMatrix((LfMatrix) typType);
                case LEAF_ENUM_e.LF_VFTABLE:          return LfVftable((LfVftable) typType);
                case LEAF_ENUM_e.LF_FUNC_ID:          return LfFuncId((LfFuncId) typType);
                case LEAF_ENUM_e.LF_MFUNC_ID:         return LfMFuncId((LfMFuncId) typType);
                case LEAF_ENUM_e.LF_BUILDINFO:        return LfBuildInfo((LfBuildInfo) typType);
                case LEAF_ENUM_e.LF_STRING_ID:        return LfStringId((LfStringId) typType);
                case LEAF_ENUM_e.LF_UDT_SRC_LINE:     return LfUdtSrcLine((LfUdtSrcLine) typType);
                case LEAF_ENUM_e.LF_UDT_MOD_SRC_LINE: return LfUdtModSrcLine((LfUdtModSrcLine) typType);
                case LEAF_ENUM_e.LF_CHAR:             return LfChar((LfChar) typType);
                case LEAF_ENUM_e.LF_SHORT:            return LfShort((LfShort) typType);
                case LEAF_ENUM_e.LF_USHORT:           return LfUShort((LfUShort) typType);
                case LEAF_ENUM_e.LF_LONG:             return LfLong((LfLong) typType);
                case LEAF_ENUM_e.LF_ULONG:            return LfULong((LfULong) typType);
                case LEAF_ENUM_e.LF_REAL32:           return LfReal32((LfReal32) typType);
                case LEAF_ENUM_e.LF_REAL64:           return LfReal64((LfReal64) typType);
                case LEAF_ENUM_e.LF_REAL80:           return LfReal80((LfReal80) typType);
                case LEAF_ENUM_e.LF_REAL128:          return LfReal128((LfReal128) typType);

                case LEAF_ENUM_e.LF_QUADWORD:
                case LEAF_ENUM_e.LF_UQUADWORD:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_REAL48:           return LfReal48((LfReal48) typType);
                case LEAF_ENUM_e.LF_COMPLEX32:        return LfCmplx32((LfCmplx32) typType);
                case LEAF_ENUM_e.LF_COMPLEX64:        return LfCmplx64((LfCmplx64) typType);
                case LEAF_ENUM_e.LF_COMPLEX80:        return LfCmplx80((LfCmplx80) typType);
                case LEAF_ENUM_e.LF_COMPLEX128:       return LfCmplx128((LfCmplx128) typType);
                case LEAF_ENUM_e.LF_VARSTRING:        return LfVarString((LfVarString) typType);

                case LEAF_ENUM_e.LF_OCTWORD:
                case LEAF_ENUM_e.LF_UOCTWORD:
                case LEAF_ENUM_e.LF_DECIMAL:
                case LEAF_ENUM_e.LF_DATE:
                case LEAF_ENUM_e.LF_UTF8STRING:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_REAL16:
                    return LfReal16((LfReal16) typType);

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
                    return LfEasy(typType);
            }
        }

        protected abstract T LfEasy(LfEasy value);

        protected abstract T LfModifier16t(LfModifier16t value);
        protected abstract T LfPointer16t(LfPointer16t value);
        protected abstract T LfArray16t(LfArray16t value);
        protected abstract T LfClass16t(LfClass16t value);
        protected abstract T LfUnion16t(LfUnion16t value);
        protected abstract T LfEnum16t(LfEnum16t value);
        protected abstract T LfProc16t(LfProc16t value);
        protected abstract T LfMFunc16t(LfMFunc16t value);
        protected abstract T LfVTShape(LfVTShape value);
        protected abstract T LfCobol016t(LfCobol016t value);
        protected abstract T LfCobol1(LfCobol1 value);
        protected abstract T LfBArray16t(LfBArray16t value);
        protected abstract T LfLabel(LfLabel value);
        protected abstract T LfDimArray16t(LfDimArray16t value);
        protected abstract T LfVFTPath16t(LfVFTPath16t value);
        protected abstract T LfPreComp16t(LfPreComp16t value);
        protected abstract T LfEndPreComp(LfEndPreComp value);
        protected abstract T LfOEM16t(LfOEM16t value);
        protected abstract T LfSkip16t(LfSkip16t value);
        protected abstract T LfArgList16t(LfArgList16t value);
        protected abstract T LfDefArg16t(LfDefArg16t value);
        protected abstract T LfList(LfList value);
        protected abstract T LfFieldList16t(LfFieldList16t value);
        protected abstract T LfDerived16t(LfDerived16t value);
        protected abstract T LfBitfield16t(LfBitfield16t value);
        protected abstract T LfMethodList16t(LfMethodList16t value);
        protected abstract T LfDimCon16t(LfDimCon16t value);
        protected abstract T LfDimVar16t(LfDimVar16t value);
        protected abstract T LfRefSym(LfRefSym value);
        protected abstract T LfBClass16t(LfBClass16t value);
        protected abstract T LfVBClass16t(LfVBClass16t value);
        protected abstract T LfFriendFcn16t(LfFriendFcn16t value);
        protected abstract T LfIndex16t(LfIndex16t value);
        protected abstract T LfMember16t(LfMember16t value);
        protected abstract T LfSTMember16t(LfSTMember16t value);
        protected abstract T LfMethod16t(LfMethod16t value);
        protected abstract T LfNestType16t(LfNestType16t value);
        protected abstract T LfVFuncTab16t(LfVFuncTab16t value);
        protected abstract T LfFriendCls16t(LfFriendCls16t value);
        protected abstract T LfOneMethod16t(LfOneMethod16t value);
        protected abstract T LfVFuncOff16t(LfVFuncOff16t value);
        protected abstract T LfModifier(LfModifier value);
        protected abstract T LfPointer(LfPointer value);
        protected abstract T LfArray(LfArray value);
        protected abstract T LfClass(LfClass value);
        protected abstract T LfUnion(LfUnion value);
        protected abstract T LfEnum(LfEnum value);
        protected abstract T LfProc(LfProc value);
        protected abstract T LfMFunc(LfMFunc value);
        protected abstract T LfCobol0(LfCobol0 value);
        protected abstract T LfBArray(LfBArray value);
        protected abstract T LfVFTPath(LfVFTPath value);
        protected abstract T LfSkip(LfSkip value);
        protected abstract T LfArgList(LfArgList value);
        protected abstract T LfFieldList(LfFieldList value);
        protected abstract T LfDerived(LfDerived value);
        protected abstract T LfBitfield(LfBitfield value);
        protected abstract T LfMethodList(LfMethodList value);
        protected abstract T LfDimCon(LfDimCon value);
        protected abstract T LfDimVar(LfDimVar value);
        protected abstract T LfBClass(LfBClass value);
        protected abstract T LfVBClass(LfVBClass value);
        protected abstract T LfIndex(LfIndex value);
        protected abstract T LfVFuncTab(LfVFuncTab value);
        protected abstract T LfFriendCls(LfFriendCls value);
        protected abstract T LfVFuncOff(LfVFuncOff value);
        protected abstract T LfTypeServer(LfTypeServer value);
        protected abstract T LfEnumerate(LfEnumerate value);
        protected abstract T LfDimArray(LfDimArray value);
        protected abstract T LfPreComp(LfPreComp value);
        protected abstract T LfAlias(LfAlias value);
        protected abstract T LfDefArg(LfDefArg value);
        protected abstract T LfFriendFcn(LfFriendFcn value);
        protected abstract T LfMember(LfMember value);
        protected abstract T LfSTMember(LfSTMember value);
        protected abstract T LfMethod(LfMethod value);
        protected abstract T LfNestType(LfNestType value);
        protected abstract T LfOneMethod(LfOneMethod value);
        protected abstract T LfNestTypeEx(LfNestTypeEx value);
        protected abstract T LfMemberModify(LfMemberModify value);
        protected abstract T LfManaged(LfManaged value);
        protected abstract T LfTypeServer2(LfTypeServer2 value);
        protected abstract T LfHLSL(LfHLSL value);
        protected abstract T LfModifierEx(LfModifierEx value);
        protected abstract T LfVector(LfVector value);
        protected abstract T LfMatrix(LfMatrix value);
        protected abstract T LfVftable(LfVftable value);
        protected abstract T LfFuncId(LfFuncId value);
        protected abstract T LfMFuncId(LfMFuncId value);
        protected abstract T LfBuildInfo(LfBuildInfo value);
        protected abstract T LfStringId(LfStringId value);
        protected abstract T LfUdtSrcLine(LfUdtSrcLine value);
        protected abstract T LfUdtModSrcLine(LfUdtModSrcLine value);
        protected abstract T LfChar(LfChar value);
        protected abstract T LfShort(LfShort value);
        protected abstract T LfUShort(LfUShort value);
        protected abstract T LfLong(LfLong value);
        protected abstract T LfULong(LfULong value);
        protected abstract T LfReal32(LfReal32 value);
        protected abstract T LfReal64(LfReal64 value);
        protected abstract T LfReal80(LfReal80 value);
        protected abstract T LfReal128(LfReal128 value);
        protected abstract T LfReal48(LfReal48 value);
        protected abstract T LfCmplx32(LfCmplx32 value);
        protected abstract T LfCmplx64(LfCmplx64 value);
        protected abstract T LfCmplx80(LfCmplx80 value);
        protected abstract T LfCmplx128(LfCmplx128 value);
        protected abstract T LfVarString(LfVarString value);
        protected abstract T LfReal16(LfReal16 value);
    }
}
