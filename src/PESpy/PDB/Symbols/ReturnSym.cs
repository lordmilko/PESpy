using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="RETURNSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ReturnSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RETURNSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_GENERIC_FLAG flags => value->flags;

        public CV_GENERIC_STYLE_e style => value->style;

        internal ReturnSym(RETURNSYM* value)
        {
            this.value = value;
        }
    }
}

