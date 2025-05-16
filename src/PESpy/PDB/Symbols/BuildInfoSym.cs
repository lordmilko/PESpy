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

        /// <inheritdoc cref="BUILDINFOSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BUILDINFOSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BUILDINFOSYM.id"/>
        public CV_ItemId id => value->id;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //id

        internal BuildInfoSym(BUILDINFOSYM* value)
        {
            this.value = value;
        }
    }
}

