using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="THUNKSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct ThunkSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly THUNKSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int pNext => value->pNext;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public short len => value->len;

        public THUNK_ORDINAL ord => (THUNK_ORDINAL) value->ord;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal ThunkSym32(THUNKSYM32* value)
        {
            this.value = value;

            byte* variant = value->name + name.Length + 1;

            //dumpsym7.cpp says adjustor and vcall can have variant
            Debug.Assert(!(ord == THUNK_ORDINAL.THUNK_ORDINAL_ADJUSTOR || ord == THUNK_ORDINAL.THUNK_ORDINAL_VCALL), "Read name and variant");
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

