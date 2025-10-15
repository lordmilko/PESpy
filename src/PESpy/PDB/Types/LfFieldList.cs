using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFieldList"/> structure.
    /// </summary>
    public readonly unsafe struct LfFieldList : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFieldList* value;

        //This type is within the range of enum values that are defined as only ever existing inside other types,
        //however this type _can_ exist as a standalone type
        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        //Our length includes the size of our "leaf" field in it
        public LfEasy[] fields => GetFields(typlen - sizeof(ushort), value->data, null);

        //We share the same logic for LfFIeldList and LfFieldList16t
        internal static LfEasy[] GetFields(int length, byte* ptr, ISymbolAccessor? symbolAccessor)
        {
            //pdbdump.cpp!strForFieldList only shows how to handle a couple of these; NT 4 shows how to handle all of them

            var pos = 0;

            using var fields = new PooledList<LfEasy>();

            while (pos < length)
            {
                var item = ptr + pos;

                var easy = new LfEasy((lfEasy*) item);

                switch (easy.leaf)
                {
                    #region LfBClass

                    case LEAF_ENUM_e.LF_BCLASS_16t:
                        var bClass16t = new LfBClass16t((lfBClass_16t*) item);
                        pos += bClass16t.StructSize;
                        fields.Add(easy);
                        break;

                    case LEAF_ENUM_e.LF_BCLASS:
                        var bClass = new LfBClass((lfBClass*) item);
                        pos += bClass.StructSize;
                        fields.Add(easy);
                        break;

                    #endregion
                    #region LfEnumerate

                    case LEAF_ENUM_e.LF_ENUMERATE:
                    case LEAF_ENUM_e.LF_ENUMERATE_ST:
                        //We need to account for the standard length of an lfEnumerate (4), the size of the value,
                        //and the length of the name, which may or may not be length prefixed
                        var enumerate = new LfEnumerate((lfEnumerate*) item);
                        pos += enumerate.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    #endregion
                    #region LfFriendCls

                    case LEAF_ENUM_e.LF_FRIENDCLS_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_FRIENDCLS:
                        throw new NotImplementedException();

                    #endregion
                    #region LfFriendFcn

                    case LEAF_ENUM_e.LF_FRIENDFCN_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_FRIENDFCN:
                    case LEAF_ENUM_e.LF_FRIENDFCN_ST:
                        throw new NotImplementedException();

                    #endregion
                    #region LfIndex

                    case LEAF_ENUM_e.LF_INDEX_16t:
                        fields.Add(easy);
                        pos += LfIndex16t.StructSize;
                        break;

                    case LEAF_ENUM_e.LF_INDEX:
                        fields.Add(easy);
                        pos += LfIndex.StructSize;
                        break;

                    #endregion

                    case LEAF_ENUM_e.LF_IVBCLASS_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_IVBCLASS:
                        throw new NotImplementedException();

                    #region LfMember

                    case LEAF_ENUM_e.LF_MEMBER_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_MEMBER:
                    case LEAF_ENUM_e.LF_MEMBER_ST:
                        var member = new LfMember((lfMember*) item);
                        pos += member.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    #endregion
                    #region LfMethod

                    case LEAF_ENUM_e.LF_METHOD_16t:
                        var method16t = new LfMethod16t((lfMethod_16t*) item);
                        pos += method16t.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    case LEAF_ENUM_e.LF_METHOD:
                    case LEAF_ENUM_e.LF_METHOD_ST:
                        var method = new LfMethod((lfMethod*) item);
                        pos += method.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    #endregion
                    #region LfNestType

                    case LEAF_ENUM_e.LF_NESTTYPE_16t:
                        var nestType16t = new LfNestType16t((lfNestType_16t*) item);
                        pos += nestType16t.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    case LEAF_ENUM_e.LF_NESTTYPE:
                    case LEAF_ENUM_e.LF_NESTTYPE_ST:
                        var nestType = new LfNestType((lfNestType*) item);
                        pos += nestType.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    #endregion
                    #region LfOneMethod

                    case LEAF_ENUM_e.LF_ONEMETHOD_16t:
                        var oneMethod16t = new LfOneMethod16t((lfOneMethod_16t*) item);
                        pos += oneMethod16t.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    case LEAF_ENUM_e.LF_ONEMETHOD:
                    case LEAF_ENUM_e.LF_ONEMETHOD_ST:
                        var oneMethod = new LfOneMethod((lfOneMethod*) item);
                        pos += oneMethod.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    #endregion
                    #region LfSTMember

                    case LEAF_ENUM_e.LF_STMEMBER_16t:
                        var staticMember16t = new LfSTMember((lfSTMember*) item);
                        pos += staticMember16t.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    case LEAF_ENUM_e.LF_STMEMBER:
                    case LEAF_ENUM_e.LF_STMEMBER_ST:
                        var staticMember = new LfSTMember((lfSTMember*) item);
                        pos += staticMember.GetStructSize(symbolAccessor);
                        fields.Add(easy);
                        break;

                    #endregion

                    case LEAF_ENUM_e.LF_VBCLASS_16t:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_VBCLASS:
                        throw new NotImplementedException();

                    case LEAF_ENUM_e.LF_VFUNCTAB_16t:
                        fields.Add(easy);
                        pos += LfVFuncTab16t.StructSize;
                        break;

                    case LEAF_ENUM_e.LF_VFUNCTAB:
                        fields.Add(easy);
                        pos += LfVFuncTab.StructSize;
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle a sub-leaf of type '{easy.leaf}'");
                }

                //Skip pad bytes
                var val = (ptr + pos);

                if (pos < length && (*val & (byte) LEAF_ENUM_e.LF_PAD0) == (byte) LEAF_ENUM_e.LF_PAD0)
                {
                    var toSkip = *val & 0xF;
                    pos += toSkip;
                }
            }

            return fields.ToArray();
        }

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfFieldList(lfFieldList* value)
        {
            this.value = value;
        }
    }
}
