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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int pNext => value->pNext;

        public CV_uoff16_t off => value->off;

        public short seg => value->seg;

        public short len => value->len;

        public THUNK_ORDINAL ord => (THUNK_ORDINAL) value->ord;

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal ThunkSym16(THUNKSYM16* value)
        {
            this.value = value;
            Debug.Assert(false, "Read name and variant");
        }
    }
}

