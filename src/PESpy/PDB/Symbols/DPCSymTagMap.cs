using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DPCSYMTAGMAP"/> structure.
    /// </summary>
    public readonly unsafe struct DPCSymTagMap : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DPCSYMTAGMAP* value;

        /// <inheritdoc cref="DPCSYMTAGMAP.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DPCSYMTAGMAP.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal DPCSymTagMap(DPCSYMTAGMAP* value)
        {
            this.value = value;
            Debug.Assert(false, "Read CV_DPC_SYM_TAG_MAP_ENTRY[] mapEntries");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DPCSYMTAGMAP, this, ViewKind.DPCSymTagMap, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
