using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMember"/> structure.
    /// </summary>
    public readonly unsafe struct LfMember : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMember* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        #region offset

        // variable length offset of field followed by length prefixed name of field

        public int offset
        {
            get
            {
                TypType.ExtractNumericData(value->offset, out var offset, out _);

                return (int) offset;
            }
        }

        public SymString name => GetName(null);

        #endregion
        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            //I am assuming I need to use normal ST/UTF parsing logic
            TypType.ExtractNumericData(value->offset, out _, out var bytesRead);

            return TypType.ReadString(value->offset + bytesRead, symbolAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2              + //attr
            sizeof(int);     //index

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(value->offset, out _, out var bytesRead);

            var str = TypType.ReadString(value->offset + bytesRead, symbolAccessor);

            return FixedStructSize + bytesRead + str.Length + 1;
        }

        internal LfMember(lfMember* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMember, this, ViewKind.LfMember, GetStructSize(writer.GetSymbolAccessor())); //Non-primary, should not have a TYPTYPE.len

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(attr), attr);
            s.WriteField(nameof(index), index);
            s.WriteNumericData(nameof(offset), value->offset);
            s.WriteSymStringField(nameof(name), GetName(viewWriter.GetSymbolAccessor()));

            //Do not align; the parent will apply padding

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
