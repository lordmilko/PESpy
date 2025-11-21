using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using static ClrDebug.PDB.LEAF_ENUM_e;

namespace PESpy.PDB
{
    public static partial class TypTypeExtensions
    {
        public static SymTagEnum? GetSymTagEnum(in this TypOrEnumType type)
        {
            var typType = type.TypTyp;

            if (typType != null)
            {
                return GetSymTagEnum(typType.Value);
            }
            else
            {
                //When the type is < 0x1000, GetData::getTypeData calls getPrimitiveTypeData

                if (type.PointerMode != CV_prmode_e.CV_TM_DIRECT)
                    return SymTagEnum.PointerType;

                return SymTagEnum.BaseType;
            }
        }

        public static SymTagEnum? GetSymTagEnum(in this TypType typType) =>
            GetSymTagEnum((LfEasy) typType);

        public static SymTagEnum? GetSymTagEnum(in this LfEasy lfEasy)
        {
            /* DIA does not return all type symbols when it enumerates types. CAllTypesTrav::next works by iterating over each type index from TiMin to TiMac
             * and then for each record calls TPI1::QueryPbCVRecordForTi(). However, not all type records get sent to the type dispatcher: certain records
             * are automatically filtered out!
             * - Values <= LF_TI16_MAX (0x1000) is considered to be 16-bit, and so are excluded
             * - Values >= 0x1200 and < LF_ST_MAX (0x1500) are considered to be leaves that are only referenced from other type records
             * - LF_TYPESERVER is also specially excluded
             * - Values >= 0x1601 (LF_FUNC_ID) and < 0x1608 (LF_CLASS2) are ID types and so are excluded. There are new v2 types in the range 0x1608+ so all of the items
             *   in this range < LF_ID_MAX are safe. Anything >= LF_ID_MAX is excluded
             */

            switch (lfEasy.leaf)
            {
                //<= LF_TI16_MAX

                //>= 0x1200 && < LF_ST_MAX + LF_TYPESERVER + 16-bit variants that would also be excluded per the above
                case LF_SKIP:
                case LF_SKIP_16t: //Not supported by DIA
                case LF_ARGLIST:
                case LF_ARGLIST_16t: //Not supported by DIA
                case LF_FIELDLIST:
                case LF_FIELDLIST_16t: //Not supported by DIA
                case LF_DERIVED:
                case LF_DERIVED_16t: //Not supported by DIA
                case LF_METHODLIST:
                case LF_METHODLIST_16t: //Not supported by DIA
                    return null;

                //Some types do have dispatchers associated with them, however these dispatchers are only called upon to build up information for an outer,
                //surfaced type. These inner types do not exist as standalone entities
                case LF_BITFIELD: //disp_LF_BITFIELD
                case LF_BITFIELD_16t: //Not supported by DIA
                    //I saw some weird evidence of DIA seeming to support fields like bitfield and the dimcon* related ones.
                    //When you've actually got a bitfield field member, the length of the field is the number of bits it occupies, its
                    //LocationType is LocIsBitField and its sym tag is data
                    return SymTagEnum.Data;

                //>= 0x1601 && < LF_CLASS2

                //>= LF_CLASS2 && < LF_ID_MAX

                //>= LF_ID_MAX
                case LF_CHAR:
                case LF_SHORT:
                case LF_USHORT:
                case LF_LONG:
                case LF_ULONG:
                case LF_REAL32:
                case LF_REAL64:
                case LF_REAL80:
                case LF_REAL128:
                case LF_QUADWORD:
                case LF_UQUADWORD:
                case LF_REAL48:
                case LF_COMPLEX32:
                case LF_COMPLEX64:
                case LF_COMPLEX80:
                case LF_COMPLEX128:
                case LF_VARSTRING:
                case LF_OCTWORD:
                case LF_UOCTWORD:
                case LF_DECIMAL:
                case LF_DATE:
                case LF_UTF8STRING:
                case LF_REAL16:
                    return null;

                case LF_ALIAS: //disp_LF_ALIAS
                case LF_ALIAS_ST: //Not supported by DIA
                    return SymTagEnum.Typedef;

                case LF_ARRAY: //disp_LF_ARRAY
                case LF_ARRAY_16t: //Not supported by DIA
                case LF_ARRAY_ST: //Not supported by DIA
                    return SymTagEnum.ArrayType;

                case LF_BARRAY:
                case LF_BARRAY_16t: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_BCLASS: //disp_LF_BINTERFACE
                case LF_BCLASS_16t: //Not supported by DIA
                    return SymTagEnum.BaseClass;

                case LF_BINTERFACE: //disp_LF_BINTERFACE
                    return SymTagEnum.BaseInterface;

                case LF_BUILDINFO:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_CLASS: //disp_LF_CLASS
                case LF_CLASS_16t: //Not supported by DIA
                case LF_CLASS_ST: //Not supported by DIA
                case LF_INTERFACE:
                case LF_STRUCTURE:
                case LF_STRUCTURE_16t: //Not supported by DIA
                case LF_STRUCTURE_ST: //Not supported by DIA
                    return SymTagEnum.UDT;

                case LF_COBOL0:
                case LF_COBOL0_16t: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_COBOL1:
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_DEFARG:
                case LF_DEFARG_16t: //Not supported by DIA
                case LF_DEFARG_ST: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_DIMARRAY: //disp_LF_DIMARRAY
                case LF_DIMARRAY_16t: //Not supported by DIA
                case LF_DIMARRAY_ST: //Not supported by DIA
                    return SymTagEnum.ArrayType;

                case LF_DIMCONLU: //disp_LF_DIMCONU
                case LF_DIMCONLU_16t: //Not supported by DIA
                case LF_DIMCONU:
                case LF_DIMCONU_16t: //Not supported by DIA
                    throw new NotImplementedException(); //Either Dimension or Data

                case LF_DIMVARLU:
                case LF_DIMVARLU_16t: //Not supported by DIA
                case LF_DIMVARU:
                case LF_DIMVARU_16t: //Not supported by DIA
                    throw new NotImplementedException(); //I only see Dimension, but CONU has Data as well so not 100% sure

                case LF_ENDPRECOMP:
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_ENUM: //disp_LF_ENUM
                case LF_ENUM_16t: //Not supported by DIA
                case LF_ENUM_ST: //Not supported by DIA
                    return SymTagEnum.Enum;

                case LF_ENUMERATE: //disp_LF_ENUMERATE
                case LF_ENUMERATE_ST: //Not supported by DIA
                    return SymTagEnum.Data;

                case LF_FRIENDCLS: //disp_LF_FRIENDCLS
                case LF_FRIENDCLS_16t: //Not supported by DIA
                    return SymTagEnum.Friend;

                case LF_FRIENDFCN: //disp_LF_FRIENDFCN
                case LF_FRIENDFCN_16t: //Not supported by DIA
                case LF_FRIENDFCN_ST: //Not supported by DIA
                    return SymTagEnum.Friend;

                case LF_FUNC_ID:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_HLSL: //disp_LF_HLSL
                    return SymTagEnum.HLSLType;

                case LF_IFC_RECORD:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_INDEX:
                case LF_INDEX_16t: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_IVBCLASS: //disp_LF_IVBCLASS
                case LF_IVBCLASS_16t: //Not supported by DIA
                    return SymTagEnum.BaseClass;

                case LF_LABEL:
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_LIST:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_MANAGED: //disp_LF_MANAGED
                case LF_MANAGED_ST: //Not supported by DIA
                    return SymTagEnum.ManagedType;

                case LF_MATRIX: //disp_LF_MATRIX
                    return SymTagEnum.MatrixType;

                case LF_MEMBER: //disp_LF_MEMBER
                case LF_MEMBER_16t: //Not supported by DIA
                case LF_MEMBER_ST: //Not supported by DIA
                    return SymTagEnum.Data;

                case LF_MEMBERMODIFY:
                case LF_MEMBERMODIFY_ST: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_METHOD: //disp_LF_METHOD
                case LF_METHOD_16t: //Not supported by DIA
                case LF_METHOD_ST: //Not supported by DIA
                    throw new NotImplementedException();

                case LF_MFUNC_ID:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_MFUNCTION: //disp_LF_MFUNCTION
                case LF_MFUNCTION_16t: //Not supported by DIA
                    return SymTagEnum.FunctionType;

                //In MicrosoftPdbSymbolModule.EnumerateTypeSymbols() I remarked that both the modified type and unmodified type represent themselves as being the same SymTagEnum.
                //I have a feeling that what's happening inside disp_LF_MODIFIER is that DIA asks the underlying type to fill in "the rest" of the fields on the SymRowImage
                case LF_MODIFIER: //disp_LF_MODIFIER
                    return ((LfModifier) lfEasy).type.GetSymTagEnum();

                case LF_MODIFIER_16t: //Not supported by DIA
                    return ((LfModifier16t) lfEasy).type.GetSymTagEnum();

                case LF_MODIFIER_EX: //disp_LF_MODIFIER_EX
                    return ((LfModifierEx) lfEasy).type.GetSymTagEnum();

                case LF_NESTTYPE: //disp_LF_NESTTYPE
                case LF_NESTTYPE_16t: //Not supported by DIA
                case LF_NESTTYPE_ST: //Not supported by DIA
                    //Something is dispatched to GetData::getTypeData, and that thing gets tagged as SymTagTypedef.
                    //Is that maybe the symbol inside the outer nested type symbol?
                    throw new NotImplementedException();

                case LF_NESTTYPEEX: //disp_LF_NESTTYPEEX
                case LF_NESTTYPEEX_ST: //Not supported by DIA
                    //Something is dispatched to GetData::getTypeData, and that thing gets tagged as SymTagTypedef.
                    //Is that maybe the symbol inside the outer nested type symbol?
                    throw new NotImplementedException();

                case LF_NOTTRAN:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_NULL:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_OEM: //disp_LF_OEM
                case LF_OEM_16t: //Not supported by DIA
                    return SymTagEnum.CustomType;

                case LF_OEM2: //disp_LF_OEM2
                    return SymTagEnum.CustomType;

                case LF_ONEMETHOD: //disp_LF_ONEMETHOD
                case LF_ONEMETHOD_16t: //Not supported by DIA
                case LF_ONEMETHOD_ST: //Not supported by DIA
                    return SymTagEnum.Function;

                case LF_PAD0:
                case LF_PAD1:
                case LF_PAD2:
                case LF_PAD3:
                case LF_PAD4:
                case LF_PAD5:
                case LF_PAD6:
                case LF_PAD7:
                case LF_PAD8:
                case LF_PAD9:
                case LF_PAD10:
                case LF_PAD11:
                case LF_PAD12:
                case LF_PAD13:
                case LF_PAD14:
                case LF_PAD15:
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_POINTER: //disp_LF_POINTER
                case LF_POINTER_16t: //Not supported by DIA
                    return SymTagEnum.PointerType;

                case LF_PRECOMP:
                case LF_PRECOMP_16t: //Not supported by DIA
                case LF_PRECOMP_ST: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_PROCEDURE: //disp_LF_PROCEDURE
                case LF_PROCEDURE_16t: //Not supported by DIA
                    return SymTagEnum.FunctionType;

                case LF_REFSYM:
                    throw new NotImplementedException();

                case LF_STMEMBER: //disp_LF_STMEMBER
                case LF_STMEMBER_16t: //Not supported by DIA
                case LF_STMEMBER_ST: //Not supported by DIA
                    return SymTagEnum.Data;

                case LF_STRIDED_ARRAY: //disp_LF_STRIDED_ARRAY
                    throw new NotImplementedException();

                case LF_STRING_ID:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_CLASS2: //disp_LF_STRUCTURE2
                case LF_STRUCTURE2:
                case LF_INTERFACE2:
                    throw new NotImplementedException();

                case LF_TAGGED_UNION:
                    return SymTagEnum.UDT;

                case LF_TUCASE:
                    return SymTagEnum.TaggedUnionCase;

                case LF_SUBSTR_LIST:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_TYPESERVER:
                case LF_TYPESERVER_ST: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_TYPESERVER2:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_UDT_MOD_SRC_LINE:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_UDT_SRC_LINE:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_UNION: //disp_LF_UNION
                case LF_UNION_16t: //Not supported by DIA
                case LF_UNION_ST: //Not supported by DIA
                    return SymTagEnum.UDT;

                case LF_UNION2: //disp_LF_UNION2
                    return SymTagEnum.UDT;

                case LF_VBCLASS: //disp_LF_VBCLASS
                case LF_VBCLASS_16t: //Not supported by DIA
                    return SymTagEnum.BaseClass;

                case LF_VECTOR: //disp_LF_VECTOR
                    return SymTagEnum.VectorType;

                case LF_VFTABLE:
                    throw new NotImplementedException(); //not specifically handled?

                case LF_VFTPATH:
                case LF_VFTPATH_16t: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_VFUNCOFF:
                case LF_VFUNCOFF_16t: //Not supported by DIA
                    throw new NotImplementedException(); //dispatched to empty method

                case LF_VFUNCTAB:
                case LF_VFUNCTAB_16t: //Not supported by DIA
                    return SymTagEnum.VTable;

                case LF_VTSHAPE: //disp_LF_VFUNCTAB
                    return SymTagEnum.VTableShape;

                default:
                    throw new NotImplementedException();
            }
        }

        public static bool IsFwdRef(this TypType typType)
        {
            /* Sometimes a given entity may actually be a forward ref. In this scenario,
             * it doesn't actually contain any fields with useful data. Later on there'll be
             * the actual non-forward ref symbol that has a field list and everything for us to inspect
             *
             * The way that DIA handles type forwarding is that during GetData::getTypeData, prior to dispatching
             * it checks whether the leaf kind is within certain "may contain type forwarding" ranges.
             *
             * In older versions of DIA, the kinds checked are
             * - LF_CLASS
             * - LF_STRUCTURE
             * - LF_UNION
             * - LF_ENUM
             *
             * - LF_INTERFACE
             *
             * - LF_CLASS2
             * - LF_STRUCTURE2
             * - LF_UNION2
             * - LF_INTERFACE2
             *
             * Which is to say, all kinds that contain a CV_prop_t that may have a fwdref property.
             *
             * In newer versions of DIA, the simple range check is replaced with a complex bit test, which curiously does not include LF_INTERFACE.
             * I haven't actually ever seen LF_INTERFACE, so I'm wondering if maybe it's legacy and they decided to remove it.
             *
             * To cater for this, we'll add an assert that checks for LF_INTERFACE specifically, so we can double check what DIA does if we ever encounter
             * a PDB that has this symbol type
             */
            switch (typType.leaf)
            {
                case LF_CLASS_16t: //Not supported by DIA
                case LF_STRUCTURE_16t: //Not supported by DIA
                    return ((LfClass16t) typType).property.fwdref;

                case LF_UNION_16t: //Not supported by DIA
                    return ((LfUnion16t) typType).property.fwdref;

                case LF_ENUM_16t: //Not supported by DIA
                    return ((LfEnum16t) typType).property.fwdref;

                case LF_CLASS:
                case LF_CLASS_ST: //Not supported by DIA
                case LF_STRUCTURE:
                case LF_STRUCTURE_ST: //Not supported by DIA
                case LF_INTERFACE:
                    Debug.Assert(typType.leaf != LF_INTERFACE); //Does Visual Studio 2022 DIA still handle interface forward refs?
                    return ((LfClass) typType).property.fwdref;

                case LF_UNION:
                case LF_UNION_ST: //Not supported by DIA
                    return ((LfUnion) typType).property.fwdref;

                case LF_ENUM:
                case LF_ENUM_ST: //Not supported by DIA
                    return ((LfEnum) typType).property.fwdref;

                case LF_CLASS2:
                case LF_STRUCTURE2:
                case LF_INTERFACE2:
                    throw new NotImplementedException(); //Don't know what the structure of these are!

                case LF_UNION2:
                    throw new NotImplementedException(); //Don't know what the structure of these are!

                case LF_TAGGED_UNION:
                    //I know that these can be forward ref'd because I see this type listed in msdia140!GetData::fetchClassFromForwardRef
                    throw new NotImplementedException();

                default:
                    return false;
            }
        }

        public static SymString GetName(in this TypType typType, ICodeViewAccessor? codeViewAccessor = null) =>
            GetName((LfEasy) typType);

        public static SymString GetName(in this LfEasy lfEasy, ICodeViewAccessor? codeViewAccessor = null)
        {
            if (!TryGetName(lfEasy, codeViewAccessor, out var name))
                throw new InvalidOperationException($"Type '{lfEasy}' does not have a name");

            return name;
        }

        public static bool TryGetName(in this TypType typType, out SymString name) =>
            TryGetName(typType, null, out name);

        public static bool TryGetName(in this TypType typType, ICodeViewAccessor? codeViewAccessor, out SymString name) =>
            TryGetName((LfEasy) typType, codeViewAccessor, out name);

        public static bool TryGetName(in this LfEasy lfEasy, out SymString name) =>
            TryGetName(lfEasy, null, out name);

        public static bool TryGetName(in this LfEasy lfEasy, ICodeViewAccessor? codeViewAccessor, out SymString name)
        {
            TypType? underlying;

            switch (lfEasy.leaf)
            {
                //LfAlias
                case LF_ALIAS:
                case LF_ALIAS_ST: //Not supported by DIA
                    name = ((LfAlias) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfArray
                case LF_ARRAY:
                case LF_ARRAY_ST: //Not supported by DIA
                    name = ((LfArray) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfArray16t
                case LF_ARRAY_16t: //Not supported by DIA
                    name = ((LfArray16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfClass16t
                case LF_CLASS_16t: //Not supported by DIA
                case LF_STRUCTURE_16t: //Not supported by DIA
                    name = ((LfClass16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfClass
                case LF_CLASS:
                case LF_CLASS_ST: //Not supported by DIA
                case LF_STRUCTURE:
                case LF_STRUCTURE_ST: //Not supported by DIA
                case LF_INTERFACE:
                    name = ((LfClass) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfOneMethod
                case LF_ONEMETHOD:
                case LF_ONEMETHOD_ST: //Not supported by DIA
                    name = ((LfOneMethod) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfOneMethod16t
                case LF_ONEMETHOD_16t: //Not supported by DIA
                    name = ((LfOneMethod16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfUnion16t
                case LF_UNION_16t: //Not supported by DIA
                    name = ((LfUnion16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfUnion
                case LF_UNION:
                case LF_UNION_ST: //Not supported by DIA
                    name = ((LfUnion) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfDimArray16t
                case LF_DIMARRAY_16t: //Not supported by DIA
                    name = ((LfDimArray16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfDimArray
                case LF_DIMARRAY:
                case LF_DIMARRAY_ST: //Not supported by DIA
                    name = ((LfDimArray) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfEnum16t
                case LF_ENUM_16t: //Not supported by DIA
                    name = ((LfEnum16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfEnum
                case LF_ENUM:
                case LF_ENUM_ST: //Not supported by DIA
                    name = ((LfEnum) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfEnumerate
                case LF_ENUMERATE:
                case LF_ENUMERATE_ST:
                    name = ((LfEnumerate) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfFriendFcn16t
                case LF_FRIENDFCN_16t: //Not supported by DIA
                    name = ((LfFriendFcn16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfFriendFcn
                case LF_FRIENDFCN:
                case LF_FRIENDFCN_ST: //Not supported by DIA
                    name = ((LfFriendFcn) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfFuncId
                case LF_FUNC_ID:
                    name = ((LfFuncId) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfManaged
                case LF_MANAGED:
                case LF_MANAGED_ST: //Not supported by DIA
                    name = ((LfManaged) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfMember16t
                case LF_MEMBER_16t: //Not supported by DIA
                    name = ((LfMember16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfMember
                case LF_MEMBER:
                case LF_MEMBER_ST: //Not supported by DIA
                    name = ((LfMember) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfMemberModify
                case LF_MEMBERMODIFY:
                case LF_MEMBERMODIFY_ST: //Not supported by DIA
                    name = ((LfMemberModify) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfMethod
                case LF_METHOD:
                case LF_METHOD_ST: //Not supported by DIA
                    name = ((LfMethod) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfMethod16t
                case LF_METHOD_16t: //Not supported by DIA
                    name = ((LfMethod16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfMFuncId
                case LF_MFUNC_ID:
                    name = ((LfMFuncId) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfNestType
                case LF_NESTTYPE:
                case LF_NESTTYPE_ST: //Not supported by DIA
                    name = ((LfNestType) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfNestType16t
                case LF_NESTTYPE_16t: //Not supported by DIA
                    name = ((LfNestType16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfNestTypeEx
                case LF_NESTTYPEEX:
                    name = ((LfNestTypeEx) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfPreComp
                case LF_PRECOMP:
                case LF_PRECOMP_ST: //Not supported by DIA
                    name = ((LfPreComp) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfPreComp16t
                case LF_PRECOMP_16t: //Not supported by DIA
                    name = ((LfPreComp16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfSTMember
                case LF_STMEMBER:
                case LF_STMEMBER_ST: //Not supported by DIA
                    name = ((LfSTMember) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfSTMember16t
                case LF_STMEMBER_16t: //Not supported by DIA
                    name = ((LfSTMember16t) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfStringId
                case LF_STRING_ID:
                    name = ((LfStringId) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfTypeServer
                case LF_TYPESERVER:
                case LF_TYPESERVER_ST: //Not supported by DIA
                    name = ((LfTypeServer) lfEasy).GetName(codeViewAccessor);
                    return true;

                //LfTypeServer2
                case LF_TYPESERVER2:
                    name = ((LfTypeServer2) lfEasy).GetName(codeViewAccessor);
                    return true;

                case LF_MODIFIER_16t: //Not supported by DIA
                    underlying = ((LfModifier16t) lfEasy).type.TypTyp;

                    if (underlying != null)
                        return TryGetName(underlying.Value, out name);
                    break;

                case LF_MODIFIER:
                    underlying = ((LfModifier) lfEasy).type.TypTyp;

                    if (underlying != null)
                        return TryGetName(underlying.Value, out name);
                    break;

                case LF_MODIFIER_EX:
                    underlying = ((LfModifierEx) lfEasy).type.TypTyp;

                    if (underlying != null)
                        return TryGetName(underlying.Value, out name);
                    break;
            }

            name = default;
            return false;
        }

        public static bool TryGetUdtKind(this TypType typType, out UdtKind udtKind)
        {
            TypType? underlying;

            switch (typType.leaf)
            {
                case LF_INTERFACE:
                case LF_INTERFACE2:
                    udtKind = UdtKind.UdtInterface;
                    return true;

                case LF_STRUCTURE:
                case LF_STRUCTURE_16t: //Not supported by DIA
                case LF_STRUCTURE_ST: //Not supported by DIA
                case LF_STRUCTURE2:
                    udtKind = UdtKind.UdtStruct;
                    return true;

                case LF_CLASS:
                case LF_CLASS_16t: //Not supported by DIA
                case LF_CLASS_ST: //Not supported by DIA
                case LF_CLASS2:
                    udtKind = UdtKind.UdtClass;
                    return true;

                case LF_UNION:
                case LF_UNION_16t: //Not supported by DIA
                case LF_UNION_ST: //Not supported by DIA
                case LF_UNION2:
                    udtKind = UdtKind.UdtUnion;
                    return true;

                case LF_TAGGED_UNION:
                    udtKind = UdtKind.UdtTaggedUnion;
                    return true;

                case LF_MODIFIER_16t: //Not supported by DIA
                    underlying = ((LfModifier16t) typType).type.TypTyp;

                    if (underlying != null)
                        return TryGetUdtKind(underlying.Value, out udtKind);
                    break;

                case LF_MODIFIER:
                    underlying = ((LfModifier) typType).type.TypTyp;

                    if (underlying != null)
                        return TryGetUdtKind(underlying.Value, out udtKind);
                    break;

                case LF_MODIFIER_EX:
                    underlying = ((LfModifierEx) typType).type.TypTyp;

                    if (underlying != null)
                        return TryGetUdtKind(underlying.Value, out udtKind);
                    break;
            }

            udtKind = default;
            return false;
        }

        public static bool TryGetBasicType(this TYPE_ENUM_e type, out BasicType basicType)
        {
            var mode = type.CV_MODE();

            if (mode != CV_prmode_e.CV_TM_DIRECT)
            {
                basicType = default;
                return false;
            }

            return TryGetPrimitiveTypeInfo(type, out _, out basicType);
        }

        public static bool TryGetLength(this TypOrEnumType type, out int length)
        {
            var primitiveType = type.PrimitiveType;

            if (primitiveType != null)
                return TryGetLength(primitiveType.Value, out length);

            return TryGetLength(type.TypTyp!.Value, out length);
        }

        public static bool TryGetLength(this TYPE_ENUM_e type, out int length) =>
            TryGetPrimitiveTypeInfo(type, out length, out _);

        //The logic of decoding the length is the same as getting the basic type, so we handle both in this method
        public static bool TryGetPrimitiveTypeInfo(this TYPE_ENUM_e type, out int length, out BasicType basicType)
        {
            basicType = default;
            var mode = type.CV_MODE();

            if (mode != CV_prmode_e.CV_TM_DIRECT)
            {
                switch (mode)
                {
                    case CV_prmode_e.CV_TM_DIRECT:
                        length = 0;
                        return true;
                    case CV_prmode_e.CV_TM_NPTR:
                        length = 2;
                        return true;
                    case CV_prmode_e.CV_TM_FPTR:
                        length = 4;
                        return true;
                    case CV_prmode_e.CV_TM_HPTR:
                        length = 4;
                        return true;
                    case CV_prmode_e.CV_TM_NPTR32:
                        length = 4;
                        return true;
                    case CV_prmode_e.CV_TM_FPTR32:
                        length = 4;
                        return true;
                    case CV_prmode_e.CV_TM_NPTR64:
                        length = 8;
                        return true;
                    case CV_prmode_e.CV_TM_NPTR128:
                        length = 8;
                        return true;

                    default:
                        length = default;
                        return false;
                }
            }
            else
            {
                //Handle special cases
                switch (type)
                {
                    case TYPE_ENUM_e.T_PINT1:
                    case TYPE_ENUM_e.T_PHINT1:
                    case TYPE_ENUM_e.T_32PFINT1:
                    case TYPE_ENUM_e.T_INT1:
                        basicType = BasicType.btInt;
                        length = 1;
                        break;

                    case TYPE_ENUM_e.T_PUINT1:
                    case TYPE_ENUM_e.T_PHUINT1:
                    case TYPE_ENUM_e.T_32PFUINT1:
                    case TYPE_ENUM_e.T_UINT1:
                        basicType = BasicType.btUInt;
                        length = 1;
                        break;

                    case TYPE_ENUM_e.T_RCHAR:
                        basicType = BasicType.btChar;
                        length = 1;
                        break;

                    case TYPE_ENUM_e.T_WCHAR:
                        basicType = BasicType.btWChar;
                        length = 2;
                        break;

                    default:
                        var cvType = type.CV_TYPE();
                        var subType = type.CV_SUBT();

                        CV_integral_e integralType;
                        CV_real_e realType;

                        switch (cvType)
                        {
                            case CV_type_e.CV_SPECIAL:
                                var specialType = (CV_special_e) subType;

                                switch (specialType)
                                {
                                    case CV_special_e.CV_SP_NOTYPE:
                                    case CV_special_e.CV_SP_ABS:
                                    case CV_special_e.CV_SP_SEGMENT:
                                    case CV_special_e.CV_SP_NOTTRANS:
                                        basicType = BasicType.btNoType;
                                        length = 0;
                                        break;

                                    case CV_special_e.CV_SP_VOID:
                                        basicType = BasicType.btVoid;
                                        length = 0;
                                        break;

                                    case CV_special_e.CV_SP_CURRENCY:
                                        basicType = BasicType.btCurrency;
                                        length = 8;
                                        break;

                                    case CV_special_e.CV_SP_NBASICSTR:
                                        basicType = BasicType.btNoType;
                                        length = 2;
                                        break;

                                    case CV_special_e.CV_SP_FBASICSTR:
                                        basicType = BasicType.btNoType;
                                        length = 4;
                                        break;

                                    case CV_special_e.CV_SP_HRESULT:
                                        basicType = BasicType.btHresult;
                                        length = 4;
                                        break;

                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                            case CV_type_e.CV_SPECIAL2:
                                var specialType2 = (CV_special2_e) subType;

                                switch (specialType2)
                                {
                                    case CV_special2_e.CV_S2_BIT:
                                        basicType = BasicType.btBit;
                                        length = 1;
                                        break;

                                    case CV_special2_e.CV_S2_PASCHAR:
                                        basicType = BasicType.btNoType;
                                        length = 0;
                                        break;

                                    case CV_special2_e.CV_S2_BOOL32FF:
                                        basicType = BasicType.btBool;
                                        length = 4;
                                        break;

                                    default:
                                        throw new NotImplementedException();
                                }

                                break;

                            case CV_type_e.CV_SIGNED:
                                integralType = (CV_integral_e) subType;

                                if (type == TYPE_ENUM_e.T_LONG)
                                    basicType = BasicType.btLong;
                                else
                                    basicType = BasicType.btInt;

                                //In CV_integral_e, each value is the number of bits to left shift "1" to get that value.
                                //e.g. CV_IN_8BYTE = 0x03. 1 << 3 = 8
                                length = 1 << (int) integralType;
                                break;

                            case CV_type_e.CV_UNSIGNED:
                                integralType = (CV_integral_e) subType;

                                if (type == TYPE_ENUM_e.T_LONG)
                                    basicType = BasicType.btULong;
                                else
                                    basicType = BasicType.btUInt;

                                length = 1 << (int) integralType;
                                break;

                            case CV_type_e.CV_BOOLEAN:
                                integralType = (CV_integral_e) subType;

                                basicType = BasicType.btBool;
                                length = 1 << (int) integralType;
                                break;

                            case CV_type_e.CV_REAL:
                                realType = (CV_real_e) subType;

                                basicType = BasicType.btFloat;
                                length = GetFloatLength(realType);
                                break;

                            case CV_type_e.CV_COMPLEX:
                                realType = (CV_real_e) subType;

                                basicType = BasicType.btComplex;
                                length = GetFloatLength(realType);
                                break;

                            case CV_type_e.CV_INT:
                                var intType = (CV_int_e) subType;

                                switch (intType)
                                {
                                    case CV_int_e.CV_RI_CHAR: //Also INT1
                                        basicType = BasicType.btChar;
                                        length = 1;
                                        break;

                                    case CV_int_e.CV_RI_WCHAR: //Also UINT1
                                        basicType = BasicType.btWChar;
                                        length = 2;
                                        break;

                                    case CV_int_e.CV_RI_INT2:
                                        basicType = BasicType.btInt;
                                        length = 2;
                                        break;

                                    case CV_int_e.CV_RI_UINT2:
                                        basicType = BasicType.btUInt;
                                        length = 2;
                                        break;

                                    case CV_int_e.CV_RI_INT4:
                                        basicType = BasicType.btInt;
                                        length = 4;
                                        break;

                                    case CV_int_e.CV_RI_UINT4:
                                        basicType = BasicType.btUInt;
                                        length = 4;
                                        break;

                                    case CV_int_e.CV_RI_INT8:
                                        basicType = BasicType.btInt;
                                        length = 8;
                                        break;

                                    case CV_int_e.CV_RI_UINT8:
                                        basicType = BasicType.btUInt;
                                        length = 8;
                                        break;

                                    case CV_int_e.CV_RI_INT16:
                                        basicType = BasicType.btInt;
                                        length = 16;
                                        break;

                                    case CV_int_e.CV_RI_UINT16:
                                        basicType = BasicType.btUInt;
                                        length = 16;
                                        break;

                                    case CV_int_e.CV_RI_CHAR16:
                                        basicType = BasicType.btChar16;
                                        length = 2;
                                        break;

                                    case CV_int_e.CV_RI_CHAR32:
                                        basicType = BasicType.btChar32;
                                        length = 4;
                                        break;

                                    default:
                                        throw new NotImplementedException();
                                }
                                break;

                        default:
                            throw new NotImplementedException();
                        }
                        break;
                }

                return true;
            }
        }

        public static bool TryGetLength(this TypType typType, out int length)
        {
            //This is not an exhaustive list
            switch (typType.leaf)
            {
                case LF_ENUM:
                case LF_ENUM_ST: //Not supported by DIA
                    return TryGetLength(((LfEnum) typType).utype, out length);

                case LF_POINTER:
                    var lfPointer = (LfPointer) typType;

                    //msdia140!getPtrData first tries to use the size specified in the record,
                    //but if the size is 0 it looks at the pointer mode instead

                    var ptr = (LfPointer) typType;
                    var size = ptr.attr.size;

                    if (size != 0)
                        length = size;
                    else
                        length = ptr.attr.ptrtype == CV_ptrtype_e.CV_PTR_64 ? 8 : 4;

                    return true;
            }

            length = default;
            return false;
        }

        private static int GetFloatLength(CV_real_e kind)
        {
            return kind switch
            {
                CV_real_e.CV_RC_REAL32 => 4,
                CV_real_e.CV_RC_REAL64 => 8,
                CV_real_e.CV_RC_REAL80 => 10,
                CV_real_e.CV_RC_REAL128 => 16,
                CV_real_e.CV_RC_REAL48 => 6,
                CV_real_e.CV_RC_REAL32PP => 4,
                CV_real_e.CV_RC_REAL16 => 2,
            };
        }
    }
}
