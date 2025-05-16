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

        /// <inheritdoc cref="RETURNSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="RETURNSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="RETURNSYM.flags"/>
        public CV_GENERIC_FLAG flags => value->flags;

        /// <inheritdoc cref="RETURNSYM.style"/>
        public CV_GENERIC_STYLE_e style => value->style;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            2              + //flags
            sizeof(byte);    //style

        internal ReturnSym(RETURNSYM* value)
        {
            this.value = value;
        }
    }
}

