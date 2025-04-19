using System;
using System.Diagnostics;
using System.Text;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    class TypTypeProxy
    {
        private TypType typType;

        public TypTypeProxy(TypType typType)
        {
            this.typType = typType;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value => GetValue(typType);

        internal static object GetValue(in TypType typType)
        {
            switch (typType.leaf)
            {
                case LEAF_ENUM_e.LF_MODIFIER_16t:   return (LfModifier16t) typType;
                case LEAF_ENUM_e.LF_POINTER_16t:    return (LfPointer16t) typType;
                case LEAF_ENUM_e.LF_ARRAY_16t:      return (LfArray16t) typType;

                case LEAF_ENUM_e.LF_CLASS_16t:
                case LEAF_ENUM_e.LF_STRUCTURE_16t:
                    return (LfClass16t) typType;

                case LEAF_ENUM_e.LF_UNION_16t:      return (LfUnion16t) typType;
                case LEAF_ENUM_e.LF_ENUM_16t:       return (LfEnum16t) typType;
                case LEAF_ENUM_e.LF_PROCEDURE_16t:  return (LfProc16t) typType;
                case LEAF_ENUM_e.LF_MFUNCTION_16t:  return (LfMFunc16t) typType;
                case LEAF_ENUM_e.LF_VTSHAPE:        return (LfVTShape) typType;
                case LEAF_ENUM_e.LF_COBOL0_16t:     return (LfCobol016t) typType;
                case LEAF_ENUM_e.LF_COBOL1:         return (LfCobol1) typType;
                case LEAF_ENUM_e.LF_BARRAY_16t:     return (LfBArray16t) typType;
                case LEAF_ENUM_e.LF_LABEL:          return (LfLabel) typType;

                case LEAF_ENUM_e.LF_NULL:
                case LEAF_ENUM_e.LF_NOTTRAN:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_DIMARRAY_16t:   return (LfDimArray16t) typType;
                case LEAF_ENUM_e.LF_VFTPATH_16t:    return (LfVFTPath16t) typType;
                case LEAF_ENUM_e.LF_PRECOMP_16t:    return (LfPreComp16t) typType;
                case LEAF_ENUM_e.LF_ENDPRECOMP:     return (LfEndPreComp) typType;
                case LEAF_ENUM_e.LF_OEM_16t:        return (LfOEM16t) typType;
                case LEAF_ENUM_e.LF_TYPESERVER_ST:  throw new NotImplementedException();
                case LEAF_ENUM_e.LF_SKIP_16t:       return (LfSkip16t) typType;
                case LEAF_ENUM_e.LF_ARGLIST_16t:    return (LfArgList16t) typType;
                case LEAF_ENUM_e.LF_DEFARG_16t:     return (LfDefArg16t) typType;
                case LEAF_ENUM_e.LF_LIST:           return (LfList) typType;
                case LEAF_ENUM_e.LF_FIELDLIST_16t:  return (LfFieldList16t) typType;
                case LEAF_ENUM_e.LF_DERIVED_16t:    return (LfDerived16t) typType;
                case LEAF_ENUM_e.LF_BITFIELD_16t:   return (LfBitfield16t) typType;
                case LEAF_ENUM_e.LF_METHODLIST_16t: throw new NotImplementedException();

                case LEAF_ENUM_e.LF_DIMCONU_16t:
                case LEAF_ENUM_e.LF_DIMCONLU_16t:
                    return (LfDimCon16t) typType;

                case LEAF_ENUM_e.LF_DIMVARU_16t:
                case LEAF_ENUM_e.LF_DIMVARLU_16t:
                    return (LfDimVar16t) typType;

                case LEAF_ENUM_e.LF_REFSYM:     return (LfRefSym) typType;
                case LEAF_ENUM_e.LF_BCLASS_16t: return (LfBClass16t) typType;

                case LEAF_ENUM_e.LF_VBCLASS_16t:
                case LEAF_ENUM_e.LF_IVBCLASS_16t:
                    return (LfVBClass16t) typType;
                
                case LEAF_ENUM_e.LF_ENUMERATE_ST:  throw new NotImplementedException();
                case LEAF_ENUM_e.LF_FRIENDFCN_16t: return (LfFriendFcn16t) typType;
                case LEAF_ENUM_e.LF_INDEX_16t:     return (LfIndex16t) typType;
                case LEAF_ENUM_e.LF_MEMBER_16t:    return (LfMember16t) typType;
                case LEAF_ENUM_e.LF_STMEMBER_16t:  return (LfSTMember16t) typType;
                case LEAF_ENUM_e.LF_METHOD_16t:    return (LfMethod16t) typType;
                case LEAF_ENUM_e.LF_NESTTYPE_16t:  return (LfNestType16t) typType;
                case LEAF_ENUM_e.LF_VFUNCTAB_16t:  return (LfVFuncTab16t) typType;
                case LEAF_ENUM_e.LF_FRIENDCLS_16t: return (LfFriendCls16t) typType;
                case LEAF_ENUM_e.LF_ONEMETHOD_16t: return (LfOneMethod16t) typType;
                case LEAF_ENUM_e.LF_VFUNCOFF_16t:  return (LfVFuncOff16t) typType;
                case LEAF_ENUM_e.LF_MODIFIER:      return (LfModifier) typType;
                case LEAF_ENUM_e.LF_POINTER:       return (LfPointer) typType;

                case LEAF_ENUM_e.LF_ARRAY_ST:
                case LEAF_ENUM_e.LF_ARRAY:
                    return (LfArray) typType;

                case LEAF_ENUM_e.LF_CLASS_ST:
                case LEAF_ENUM_e.LF_STRUCTURE_ST:
                case LEAF_ENUM_e.LF_CLASS:
                case LEAF_ENUM_e.LF_STRUCTURE:
                case LEAF_ENUM_e.LF_INTERFACE:
                    return (LfClass) typType;

                case LEAF_ENUM_e.LF_UNION_ST:
                case LEAF_ENUM_e.LF_UNION:
                    return (LfUnion) typType;

                case LEAF_ENUM_e.LF_ENUM_ST:
                case LEAF_ENUM_e.LF_ENUM:
                    return (LfEnum) typType;

                case LEAF_ENUM_e.LF_PROCEDURE:   return (LfProc) typType;
                case LEAF_ENUM_e.LF_MFUNCTION:   return (LfMFunc) typType;
                case LEAF_ENUM_e.LF_COBOL0:      return (LfCobol0) typType;
                case LEAF_ENUM_e.LF_BARRAY:      return (LfBArray) typType;
                case LEAF_ENUM_e.LF_DIMARRAY_ST: throw new NotImplementedException();
                case LEAF_ENUM_e.LF_VFTPATH:     return (LfVFTPath) typType;

                case LEAF_ENUM_e.LF_PRECOMP_ST:
                case LEAF_ENUM_e.LF_OEM:
                case LEAF_ENUM_e.LF_ALIAS_ST:
                case LEAF_ENUM_e.LF_OEM2:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_SKIP:      return (LfSkip) typType;

                case LEAF_ENUM_e.LF_ARGLIST:
                case LEAF_ENUM_e.LF_SUBSTR_LIST:
                    return (LfArgList) typType;

                case LEAF_ENUM_e.LF_DEFARG_ST:  throw new NotImplementedException();
                case LEAF_ENUM_e.LF_FIELDLIST:  return (LfFieldList) typType;
                case LEAF_ENUM_e.LF_DERIVED:    return (LfDerived) typType;
                case LEAF_ENUM_e.LF_BITFIELD:   return (LfBitfield) typType;
                case LEAF_ENUM_e.LF_METHODLIST: return (LfMethodList) typType;

                case LEAF_ENUM_e.LF_DIMCONU:
                case LEAF_ENUM_e.LF_DIMCONLU:
                    return (LfDimCon) typType;

                case LEAF_ENUM_e.LF_DIMVARU:
                case LEAF_ENUM_e.LF_DIMVARLU:
                    return (LfDimVar) typType;

                case LEAF_ENUM_e.LF_BCLASS:
                case LEAF_ENUM_e.LF_BINTERFACE:
                    return (LfBClass) typType;

                case LEAF_ENUM_e.LF_VBCLASS:
                    return (LfVBClass) typType;

                case LEAF_ENUM_e.LF_IVBCLASS:
                case LEAF_ENUM_e.LF_FRIENDFCN_ST:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_INDEX: return (LfIndex) typType;

                case LEAF_ENUM_e.LF_MEMBER_ST:
                case LEAF_ENUM_e.LF_STMEMBER_ST:
                case LEAF_ENUM_e.LF_METHOD_ST:
                case LEAF_ENUM_e.LF_NESTTYPE_ST:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_VFUNCTAB:     return (LfVFuncTab) typType;
                case LEAF_ENUM_e.LF_FRIENDCLS:    return (LfFriendCls) typType;
                case LEAF_ENUM_e.LF_ONEMETHOD_ST: throw new NotImplementedException();
                case LEAF_ENUM_e.LF_VFUNCOFF:     return (LfVFuncOff) typType;

                case LEAF_ENUM_e.LF_NESTTYPEEX_ST:
                case LEAF_ENUM_e.LF_MEMBERMODIFY_ST:
                case LEAF_ENUM_e.LF_MANAGED_ST:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_TYPESERVER:       return (LfTypeServer) typType;
                case LEAF_ENUM_e.LF_ENUMERATE:        return (LfEnumerate) typType;

                case LEAF_ENUM_e.LF_DIMARRAY:         return (LfDimArray) typType;
                case LEAF_ENUM_e.LF_PRECOMP:          throw new NotImplementedException();
                case LEAF_ENUM_e.LF_ALIAS:            throw new NotImplementedException();
                case LEAF_ENUM_e.LF_DEFARG:           return (LfDefArg) typType;
                case LEAF_ENUM_e.LF_FRIENDFCN:        return (LfFriendFcn) typType;
                case LEAF_ENUM_e.LF_MEMBER:           return (LfMember) typType;
                case LEAF_ENUM_e.LF_STMEMBER:         return (LfSTMember) typType;
                case LEAF_ENUM_e.LF_METHOD:           return (LfMethod) typType;
                case LEAF_ENUM_e.LF_NESTTYPE:         return (LfNestType) typType;
                case LEAF_ENUM_e.LF_ONEMETHOD:        return (LfOneMethod) typType;
                case LEAF_ENUM_e.LF_NESTTYPEEX:       return (LfNestTypeEx) typType;
                case LEAF_ENUM_e.LF_MEMBERMODIFY:     return (LfMemberModify) typType;
                case LEAF_ENUM_e.LF_MANAGED:          return (LfManaged) typType;
                case LEAF_ENUM_e.LF_TYPESERVER2:      return (LfTypeServer2) typType;
                case LEAF_ENUM_e.LF_STRIDED_ARRAY:    throw new NotImplementedException();
                case LEAF_ENUM_e.LF_HLSL:             return (LfHLSL) typType;
                case LEAF_ENUM_e.LF_MODIFIER_EX:      return (LfModifierEx) typType;
                case LEAF_ENUM_e.LF_VECTOR:           return (LfVector) typType;
                case LEAF_ENUM_e.LF_MATRIX:           return (LfMatrix) typType;
                case LEAF_ENUM_e.LF_VFTABLE:          return (LfVftable) typType;
                case LEAF_ENUM_e.LF_FUNC_ID:          return (LfFuncId) typType;
                case LEAF_ENUM_e.LF_MFUNC_ID:         return (LfMFuncId) typType;
                case LEAF_ENUM_e.LF_BUILDINFO:        return (LfBuildInfo) typType;
                case LEAF_ENUM_e.LF_STRING_ID:        return (LfStringId) typType;
                case LEAF_ENUM_e.LF_UDT_SRC_LINE:     return (LfUdtSrcLine) typType;
                case LEAF_ENUM_e.LF_UDT_MOD_SRC_LINE: return (LfUdtModSrcLine) typType;
                case LEAF_ENUM_e.LF_CHAR:             return (LfChar) typType;
                case LEAF_ENUM_e.LF_SHORT:            return (LfShort) typType;
                case LEAF_ENUM_e.LF_USHORT:           return (LfUShort) typType;
                case LEAF_ENUM_e.LF_LONG:             return (LfLong) typType;
                case LEAF_ENUM_e.LF_ULONG:            return (LfULong) typType;
                case LEAF_ENUM_e.LF_REAL32:           return (LfReal32) typType;
                case LEAF_ENUM_e.LF_REAL64:           return (LfReal64) typType;
                case LEAF_ENUM_e.LF_REAL80:           return (LfReal80) typType;
                case LEAF_ENUM_e.LF_REAL128:          return (LfReal128) typType;

                case LEAF_ENUM_e.LF_QUADWORD:
                case LEAF_ENUM_e.LF_UQUADWORD:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_REAL48:           return (LfReal48) typType;
                case LEAF_ENUM_e.LF_COMPLEX32:        return (LfCmplx32) typType;
                case LEAF_ENUM_e.LF_COMPLEX64:        return (LfCmplx64) typType;
                case LEAF_ENUM_e.LF_COMPLEX80:        return (LfCmplx80) typType;
                case LEAF_ENUM_e.LF_COMPLEX128:       return (LfCmplx128) typType;
                case LEAF_ENUM_e.LF_VARSTRING:        return (LfVarString) typType;

                case LEAF_ENUM_e.LF_OCTWORD:
                case LEAF_ENUM_e.LF_UOCTWORD:
                case LEAF_ENUM_e.LF_DECIMAL:
                case LEAF_ENUM_e.LF_DATE:
                case LEAF_ENUM_e.LF_UTF8STRING:
                    throw new NotImplementedException();

                case LEAF_ENUM_e.LF_REAL16:
                    return (LfReal16) typType;

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
                    return typType;
            }
        }

        public static string GetString(TypType typType)
        {
            var underlying = GetValue(typType);

            //TypType.ToString() calls back into GetString again, so we need to special case bail out
            //if there wasn't a more specialized TypType implementation
            if (underlying is TypType t)
                return t.leaf.ToString();

            return underlying.ToString();
        }

        public static string DebuggerDisplay(TypType typType)
        {
            var builder = new StringBuilder();
            builder.Append("[").Append(typType.leaf).Append("]");

            var value = GetValue(typType);

            var defaultStr = typType.leaf.ToString();

            var str = value.ToString();

            if (defaultStr != str)
                builder.Append(" ").Append(str);

            return builder.ToString();
        }
    }
}
