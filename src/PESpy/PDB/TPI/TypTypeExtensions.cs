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

        public static SymTagEnum? GetSymTagEnum(in this TypType typType)
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

            //todo: well what about lf_bitfield? dia DOES handle it despite it being 0x1205
            //the dimcon* ones as well

            switch (typType.leaf)
            {
                //<= LF_TI16_MAX

                //>= 0x1200 && < LF_ST_MAX + LF_TYPESERVER + 16-bit variants that would also be excluded per the above
                case LF_SKIP:
                case LF_SKIP_16t:
                case LF_ARGLIST:
                case LF_ARGLIST_16t:
                case LF_FIELDLIST:
                case LF_FIELDLIST_16t:
                case LF_DERIVED:
                case LF_DERIVED_16t:
                case LF_METHODLIST:
                case LF_METHODLIST_16t:
                    return null;

                //Some types do have dispatchers associated with them, however these dispatchers are only called upon to build up information for an outer,
                //surfaced type. These inner types do not exist as standalone entities
                case LF_BITFIELD: //disp_LF_BITFIELD
                case LF_BITFIELD_16t:
                    return null;

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
                case LF_ALIAS_ST:
                    return SymTagEnum.Typedef;

                case LF_ARRAY: //disp_LF_ARRAY
                case LF_ARRAY_16t:
                case LF_ARRAY_ST:
                    return SymTagEnum.ArrayType;
                case LF_CLASS: //disp_LF_CLASS
                case LF_CLASS_16t:
                case LF_CLASS_ST:
                case LF_INTERFACE:
                case LF_STRUCTURE:
                case LF_STRUCTURE_16t:
                case LF_STRUCTURE_ST:
                    return SymTagEnum.UDT;
                case LF_ENUM: //disp_LF_ENUM
                case LF_ENUM_16t:
                case LF_ENUM_ST:
                    return SymTagEnum.Enum;

                case LF_ENUMERATE: //disp_LF_ENUMERATE
                case LF_ENUMERATE_ST:
                    return SymTagEnum.Data;

                case LF_FRIENDCLS: //disp_LF_FRIENDCLS
                case LF_FRIENDCLS_16t:
                    return SymTagEnum.Friend;

                case LF_FRIENDFCN: //disp_LF_FRIENDFCN
                case LF_FRIENDFCN_16t:
                case LF_FRIENDFCN_ST:
                    return SymTagEnum.Friend;
                case LF_MANAGED: //disp_LF_MANAGED
                case LF_MANAGED_ST:
                    return SymTagEnum.ManagedType;

                case LF_MATRIX: //disp_LF_MATRIX
                    return SymTagEnum.MatrixType;

                case LF_MEMBER: //disp_LF_MEMBER
                case LF_MEMBER_16t:
                case LF_MEMBER_ST:
                    return SymTagEnum.Data;
                case LF_MFUNCTION: //disp_LF_MFUNCTION
                case LF_MFUNCTION_16t:
                    return SymTagEnum.FunctionType;

                //In MicrosoftPdbSymbolModule.EnumerateTypeSymbols() I remarked that both the modified type and unmodified type represent themselves as being the same SymTagEnum.
                //I have a feeling that what's happening inside disp_LF_MODIFIER is that DIA asks the underlying type to fill in "the rest" of the fields on the SymRowImage
                case LF_MODIFIER: //disp_LF_MODIFIER
                    return ((LfModifier) typType).type.GetSymTagEnum();

                case LF_MODIFIER_16t:
                    return ((LfModifier16t) typType).type.GetSymTagEnum();

                case LF_MODIFIER_EX: //disp_LF_MODIFIER_EX
                    return ((LfModifierEx) typType).type.GetSymTagEnum();
                case LF_UNION: //disp_LF_UNION
                case LF_UNION_16t:
                case LF_UNION_ST:
                    return SymTagEnum.UDT;

                case LF_UNION2: //disp_LF_UNION2
                    return SymTagEnum.UDT;

                case LF_VBCLASS: //disp_LF_VBCLASS
                case LF_VBCLASS_16t:
                    return SymTagEnum.BaseClass;

                case LF_VECTOR: //disp_LF_VECTOR
                    return SymTagEnum.VectorType;
                case LF_VFUNCTAB:
                case LF_VFUNCTAB_16t:
                    return SymTagEnum.VTable;

                case LF_VTSHAPE: //disp_LF_VFUNCTAB
                    return SymTagEnum.VTableShape;
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
                case LF_CLASS_16t:
                case LF_STRUCTURE_16t:
                    return ((LfClass16t) typType).property.fwdref;

                case LF_UNION_16t:
                    return ((LfUnion16t) typType).property.fwdref;

                case LF_ENUM_16t:
                    return ((LfEnum16t) typType).property.fwdref;

                case LF_CLASS:
                case LF_CLASS_ST:
                case LF_STRUCTURE:
                case LF_STRUCTURE_ST:
                case LF_INTERFACE:
                    Debug.Assert(typType.leaf != LF_INTERFACE); //Does Visual Studio 2022 DIA still handle interface forward refs?
                    return ((LfClass) typType).property.fwdref;

                case LF_UNION:
                case LF_UNION_ST:
                    return ((LfUnion) typType).property.fwdref;

                case LF_ENUM:
                case LF_ENUM_ST:
                    return ((LfEnum) typType).property.fwdref;
                default:
                    return false;
            }
        }

        public static SymString GetName(in this TypType typType) => GetName(typType, null);
        public static bool TryGetName(in this TypType typType, out SymString name) =>
            TryGetName(typType, null, out name);

        internal static bool TryGetName(in this TypType typType, ISymbolAccessor? symbolAccessor, out SymString name)
        {
            TypType? underlying;

            switch (typType.leaf)
            {
                //LfAlias
                case LF_ALIAS:
                case LF_ALIAS_ST:
                    name = ((LfAlias) typType).GetName(symbolAccessor);
                    return true;

                //LfArray
                case LF_ARRAY:
                case LF_ARRAY_ST:
                    name = ((LfArray) typType).GetName(symbolAccessor);
                    return true;

                //LfArray16t
                case LF_ARRAY_16t:
                    name = ((LfArray16t) typType).GetName(symbolAccessor);
                    return true;

                //LfClass16t
                case LF_CLASS_16t:
                case LF_STRUCTURE_16t:
                    name = ((LfClass16t) typType).GetName(symbolAccessor);
                    return true;

                //LfClass
                case LF_CLASS:
                case LF_CLASS_ST:
                case LF_STRUCTURE:
                case LF_STRUCTURE_ST:
                case LF_INTERFACE:
                    name = ((LfClass) typType).GetName(symbolAccessor);
                    return true;

                //LfOneMethod
                case LF_ONEMETHOD:
                case LF_ONEMETHOD_ST:
                    name = ((LfOneMethod) typType).GetName(symbolAccessor);
                    return true;

                //LfOneMethod16t
                case LF_ONEMETHOD_16t:
                    name = ((LfOneMethod16t) typType).GetName(symbolAccessor);
                    return true;

                //LfUnion16t
                case LF_UNION_16t:
                    name = ((LfUnion16t) typType).GetName(symbolAccessor);
                    return true;

                //LfUnion
                case LF_UNION:
                case LF_UNION_ST:
                    name = ((LfUnion) typType).GetName(symbolAccessor);
                    return true;

                //LfDimArray16t
                case LF_DIMARRAY_16t:
                    name = ((LfDimArray16t) typType).GetName(symbolAccessor);
                    return true;

                //LfDimArray
                case LF_DIMARRAY:
                case LF_DIMARRAY_ST:
                    name = ((LfDimArray) typType).GetName(symbolAccessor);
                    return true;

                //LfEnum16t
                case LF_ENUM_16t:
                    name = ((LfEnum16t) typType).GetName(symbolAccessor);
                    return true;

                //LfEnum
                case LF_ENUM:
                case LF_ENUM_ST:
                    name = ((LfEnum) typType).GetName(symbolAccessor);
                    return true;

                //LfFriendFcn16t
                case LF_FRIENDFCN_16t:
                    name = ((LfFriendFcn16t) typType).GetName(symbolAccessor);
                    return true;

                //LfFriendFcn
                case LF_FRIENDFCN:
                case LF_FRIENDFCN_ST:
                    name = ((LfFriendFcn) typType).GetName(symbolAccessor);
                    return true;

                //LfFuncId
                case LF_FUNC_ID:
                    name = ((LfFuncId) typType).GetName(symbolAccessor);
                    return true;

                //LfManaged
                case LF_MANAGED:
                case LF_MANAGED_ST:
                    name = ((LfManaged) typType).GetName(symbolAccessor);
                    return true;

                //LfMember16t
                case LF_MEMBER_16t:
                    name = ((LfMember16t) typType).GetName(symbolAccessor);
                    return true;

                //LfMember
                case LF_MEMBER:
                case LF_MEMBER_ST:
                    name = ((LfMember) typType).GetName(symbolAccessor);
                    return true;

                //LfMemberModify
                case LF_MEMBERMODIFY:
                case LF_MEMBERMODIFY_ST:
                    name = ((LfMemberModify) typType).GetName(symbolAccessor);
                    return true;

                //LfMethod
                case LF_METHOD:
                case LF_METHOD_ST:
                    name = ((LfMethod) typType).GetName(symbolAccessor);
                    return true;

                //LfMethod16t
                case LF_METHOD_16t:
                    name = ((LfMethod16t) typType).GetName(symbolAccessor);
                    return true;

                //LfMFuncId
                case LF_MFUNC_ID:
                    name = ((LfMFuncId) typType).GetName(symbolAccessor);
                    return true;

                //LfNestType
                case LF_NESTTYPE:
                case LF_NESTTYPE_ST:
                    name = ((LfNestType) typType).GetName(symbolAccessor);
                    return true;

                //LfNestType16t
                case LF_NESTTYPE_16t:
                    name = ((LfNestType16t) typType).GetName(symbolAccessor);
                    return true;

                //LfNestTypeEx
                case LF_NESTTYPEEX:
                    name = ((LfNestTypeEx) typType).GetName(symbolAccessor);
                    return true;

                //LfPreComp
                case LF_PRECOMP:
                case LF_PRECOMP_ST:
                    name = ((LfPreComp) typType).GetName(symbolAccessor);
                    return true;

                //LfPreComp16t
                case LF_PRECOMP_16t:
                    name = ((LfPreComp16t) typType).GetName(symbolAccessor);
                    return true;

                //LfSTMember
                case LF_STMEMBER:
                case LF_STMEMBER_ST:
                    name = ((LfSTMember) typType).GetName(symbolAccessor);
                    return true;

                //LfSTMember16t
                case LF_STMEMBER_16t:
                    name = ((LfSTMember16t) typType).GetName(symbolAccessor);
                    return true;

                //LfStringId
                case LF_STRING_ID:
                    name = ((LfStringId) typType).GetName(symbolAccessor);
                    return true;

                //LfTypeServer
                case LF_TYPESERVER:
                case LF_TYPESERVER_ST:
                    name = ((LfTypeServer) typType).GetName(symbolAccessor);
                    return true;

                //LfTypeServer2
                case LF_TYPESERVER2:
                    name = ((LfTypeServer2) typType).GetName(symbolAccessor);
                    return true;

                case LF_MODIFIER_16t:
                    underlying = ((LfModifier16t) typType).type.TypTyp;

                    if (underlying != null)
                        return TryGetName(underlying.Value, out name);
                    break;

                case LF_MODIFIER:
                    underlying = ((LfModifier) typType).type.TypTyp;

                    if (underlying != null)
                        return TryGetName(underlying.Value, out name);
                    break;

                case LF_MODIFIER_EX:
                    underlying = ((LfModifierEx) typType).type.TypTyp;

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
                case LF_STRUCTURE_16t:
                case LF_STRUCTURE_ST:
                case LF_STRUCTURE2:
                    udtKind = UdtKind.UdtStruct;
                    return true;

                case LF_CLASS:
                case LF_CLASS_16t:
                case LF_CLASS_ST:
                case LF_CLASS2:
                    udtKind = UdtKind.UdtClass;
                    return true;

                case LF_UNION:
                case LF_UNION_16t:
                case LF_UNION_ST:
                case LF_UNION2:
                    udtKind = UdtKind.UdtUnion;
                    return true;

                case LF_TAGGED_UNION:
                    udtKind = UdtKind.UdtTaggedUnion;
                    return true;

                case LF_MODIFIER_16t:
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
                case LF_ENUM_ST:
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
