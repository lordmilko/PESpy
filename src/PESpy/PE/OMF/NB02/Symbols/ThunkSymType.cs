using System;
using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="THUNKSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct ThunkSymType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="THUNKSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="THUNKSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="THUNKSYMTYPE.parentsym"/>
        public int parentsym => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.endsym"/>
        public int endsym => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.nextsym"/>
        public int nextsym => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.ord"/>
        public byte ord => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.off"/>
        public int off => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.seg"/>
        public ushort seg => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.len"/>
        public short len => throw new NotImplementedException();

        /// <inheritdoc cref="THUNKSYMTYPE.name"/>
        public SymString name => throw new NotImplementedException();

        internal ThunkSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
