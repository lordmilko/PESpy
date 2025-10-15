using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OBJNAMESYM"/> structure.
    /// </summary>
    public readonly unsafe struct ObjNameSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OBJNAMESYM* value;

        /// <inheritdoc cref="OBJNAMESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="OBJNAMESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="OBJNAMESYM.signature"/>
        public int signature => value->signature;

        /// <inheritdoc cref="OBJNAMESYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //signature

        internal ObjNameSym(OBJNAMESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.OBJNAMESYM, this, ViewKind.ObjNameSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(signature), signature);
            s.WriteSymStringField(nameof(name), SymType.ReadString(value, value->name, viewWriter.GetSymbolAccessor()));

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
