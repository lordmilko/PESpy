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

        /// <inheritdoc cref="THUNKSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="THUNKSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="THUNKSYM32.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="THUNKSYM32.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="THUNKSYM32.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="THUNKSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="THUNKSYM32.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="THUNKSYM32.len"/>
        public short len => value->len;

        /// <inheritdoc cref="THUNKSYM32.ord"/>
        public THUNK_ORDINAL ord => (THUNK_ORDINAL) value->ord;

        /// <inheritdoc cref="THUNKSYM32.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //len
            sizeof(byte);    //ord

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

