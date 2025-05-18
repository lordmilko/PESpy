using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRMANYREGSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct AttrManyRegSym2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRMANYREGSYM2* value;

        /// <inheritdoc cref="ATTRMANYREGSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRMANYREGSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRMANYREGSYM2.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="ATTRMANYREGSYM2.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRMANYREGSYM2.count"/>
        public short count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            8              + //attr
            sizeof(short);   //count

        internal AttrManyRegSym2(ATTRMANYREGSYM2* value)
        {
            this.value = value;
            Debug.Assert(false, "Implement reg and name, which are both variable length arrays"); //CV_HREG_e?
        }
    }
}

