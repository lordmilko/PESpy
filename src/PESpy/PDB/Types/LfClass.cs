using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfClass : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfClass* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        public TypOrEnumType derived => new TypOrEnumType((byte*) value, value->derived);

        public TypOrEnumType vshape => new TypOrEnumType((byte*) value, value->vshape);

        #region data

        //"data" describes the length of the structure in bytes, and name. In addition, if property.hasuniquename is set,
        //there is a decorated name following the name

        public int length
        {
            get
            {
                //Length may be 0, this is normal
                TypType.ExtractNumericData(value->data, out var length, out var bytesRead);

                return (int) length;
            }
        }

        public SymString name => GetName(null);

        public SymString uniquename => GetUniqueName(null);

        #endregion
        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(value->data, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(value->data + bytesRead, symbolAccessor);
        }

        internal SymString GetUniqueName(ISymbolAccessor? symbolAccessor)
        {
            if (property.hasuniquename)
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                //I am assuming I need to use normal ST/UTF parsing logic
                var name = TypType.ReadString(value->data + bytesRead, symbolAccessor);

                //I am assuming I need to use normal ST/UTF parsing logic
                return TypType.ReadString(value->data + bytesRead + name.Length + 1, symbolAccessor); //+1 because it's either null terminated or length prefixed
            }

            return default;
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            2              + //property
            sizeof(int)    + //field
            sizeof(int)    + //derived
            sizeof(int);     //vshape

        internal LfClass(lfClass* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfClass, this, ViewKind.LfClass, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(count), count);
            s.WriteField(nameof(property), property);
            s.WriteField(nameof(field), field);
            s.WriteField(nameof(derived), derived);
            s.WriteField(nameof(vshape), vshape);
            s.WriteNumericData(nameof(length), value->data);
            s.WriteSymStringField(nameof(name), GetName(viewWriter.GetSymbolAccessor()));

            if (property.hasuniquename)
                s.WriteSymStringField(nameof(uniquename), GetUniqueName(viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
