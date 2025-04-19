using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BUILDINFOSYM"/> structure.
    /// </summary>
    public readonly unsafe struct BuildInfoSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BUILDINFOSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_ItemId id => value->id;

        internal BuildInfoSym(BUILDINFOSYM* value)
        {
            this.value = value;
        }
    }
}

