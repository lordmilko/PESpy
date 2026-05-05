using System;
using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="CV4BLKSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct CV4BlkSymType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="CV4BLKSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="CV4BLKSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="CV4BLKSYMTYPE.parentsym"/>
        public int parentsym => throw new NotImplementedException();

        /// <inheritdoc cref="CV4BLKSYMTYPE.endsym"/>
        public int endsym => throw new NotImplementedException();

        /// <inheritdoc cref="CV4BLKSYMTYPE.off"/>
        public int off => throw new NotImplementedException();

        /// <inheritdoc cref="CV4BLKSYMTYPE.seg"/>
        public ushort seg => throw new NotImplementedException();

        /// <inheritdoc cref="CV4BLKSYMTYPE.len"/>
        public int len => throw new NotImplementedException();

        /// <inheritdoc cref="CV4BLKSYMTYPE.name"/>
        public SymString name => throw new NotImplementedException();

        internal CV4BlkSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
