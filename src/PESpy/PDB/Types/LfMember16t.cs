using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMember_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMember16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMember_16t* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        public CV_fldattr_t attr => value->attr;

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => throw new System.NotImplementedException(); //TypType.ReadString(value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //index
            2;               //attr

        internal LfMember16t(lfMember_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read offset");
        }
        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(index), index);
            s.WriteField(nameof(attr), attr);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
