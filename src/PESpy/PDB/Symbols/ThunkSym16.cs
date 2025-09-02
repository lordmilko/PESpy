using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="THUNKSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct ThunkSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly THUNKSYM16* value;

        /// <inheritdoc cref="THUNKSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="THUNKSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="THUNKSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="THUNKSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="THUNKSYM16.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="THUNKSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="THUNKSYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="THUNKSYM16.len"/>
        public short len => value->len;

        /// <inheritdoc cref="THUNKSYM16.ord"/>
        public THUNK_ORDINAL ord => (THUNK_ORDINAL) value->ord;

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short)  + //len
            sizeof(byte);    //ord

        internal ThunkSym16(THUNKSYM16* value)
        {
            this.value = value;
            Debug.Assert(false, "Read name and variant");
        }
    }
}

