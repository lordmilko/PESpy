using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnumerate"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnumerate : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnumerate* raw;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => raw->leaf;

        public CV_fldattr_t attr => raw->attr;

        public ulong value
        {
            get
            {
                TypType.ExtractNumericData(raw->value, out var value, out _);

                return value;
            }
        }

        public SymString name => GetName(null);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(raw->value, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(raw->value + bytesRead, symbolAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            2;               //attr

        internal int StructSize => GetStructSize(null);

        internal int GetStructSize(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(raw->value, out _, out var bytesRead);

            var str = TypType.ReadString(raw->value + bytesRead, symbolAccessor);

            return FixedStructSize + bytesRead + str.Length + 1;
        }

        internal LfEnumerate(lfEnumerate* value)
        {
            this.raw = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfEnumerate, this, ViewKind.LfEnumerate, GetStructSize(writer.GetSymbolAccessor())); //Non-primary, should not have a TYPTYPE.len

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(attr), attr);
            s.WriteNumericData(nameof(value), raw->value);
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
