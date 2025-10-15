using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CONSTSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ConstSym16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CONSTSYM_16t* raw;

        /// <inheritdoc cref="CONSTSYM_16t.reclen"/>
        public ushort reclen => raw->reclen;

        /// <inheritdoc cref="CONSTSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => raw->rectyp;

        /// <inheritdoc cref="CONSTSYM_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) raw, raw->typind);

        /// <inheritdoc cref="CONSTSYM_16t.value"/>
        public ulong value
        {
            get
            {
                //Length may be 0, this is normal
                TypType.ExtractNumericData((byte*) &raw->value, out var value, out _);

                return value;
            }
        }

        /// <inheritdoc cref="CONSTSYM_16t.name"/>
        public SymString name => SymType.ReadString(raw, raw->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData((byte*) &raw->value, out _, out var bytesRead);

            return SymType.ReadString(raw, (byte*) &raw->value + bytesRead, symbolAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //typind
            sizeof(short);   //value

        internal ConstSym16t(CONSTSYM_16t* value)
        {
            this.raw = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CONSTSYM_16t, this, ViewKind.ConstSym16t, SymType.GetSymbolLength((SYMTYPE*) raw, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
            s.WriteNumericData(nameof(value), (byte*) &raw->value);
            s.WriteSymStringField(nameof(name), GetName(viewWriter.GetSymbolAccessor()));

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
