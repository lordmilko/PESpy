using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnum"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnum : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnum* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public TypOrEnumType utype => new TypOrEnumType((byte*) value, value->utype);

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        public SymString Name => TypType.ReadString(value->Name);

        public SymString uniquename => GetUniqueName(null);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => TypType.ReadString(value->Name, symbolAccessor);

        internal SymString GetUniqueName(ISymbolAccessor? symbolAccessor)
        {
            if (property.hasuniquename)
            {
                var name = TypType.ReadString(value->Name, symbolAccessor);

                var uniqueName = TypType.ReadString(value->Name + name.Length + 1, symbolAccessor);

                return uniqueName;
            }

            return default;
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            2              + //property
            sizeof(int)    + //utype
            sizeof(int);     //field

        internal LfEnum(lfEnum* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEnum, this, ViewKind.LfEnum, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(count), count);
            s.WriteField(nameof(property), property);
            s.WriteField(nameof(utype), utype);
            s.WriteField(nameof(field), field);
            s.WriteSymStringField(nameof(Name), TypType.ReadString(value->Name, viewWriter.GetSymbolAccessor()));

            if (property.hasuniquename)
                s.WriteSymStringField(nameof(uniquename), GetUniqueName(viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
