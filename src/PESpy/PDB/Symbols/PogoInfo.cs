using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="POGOINFO"/> structure.
    /// </summary>
    public readonly unsafe struct PogoInfo : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly POGOINFO* value;

        /// <inheritdoc cref="POGOINFO.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="POGOINFO.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="POGOINFO.invocations"/>
        public int invocations => value->invocations;

        /// <inheritdoc cref="POGOINFO.dynCount"/>
        public long dynCount => value->dynCount;

        /// <inheritdoc cref="POGOINFO.numInstrs"/>
        public int numInstrs => value->numInstrs;

        /// <inheritdoc cref="POGOINFO.staInstLive"/>
        public int staInstLive => value->staInstLive;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //invocations
            sizeof(long)   + //dynCount
            sizeof(int)    + //numInstrs
            sizeof(int);     //staInstLive

        internal PogoInfo(POGOINFO* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.POGOINFO, this, ViewKind.PogoInfo, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(invocations), invocations);
            s.WriteField(nameof(dynCount), dynCount);
            s.WriteField(nameof(numInstrs), numInstrs);
            s.WriteField(nameof(staInstLive), staInstLive);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
