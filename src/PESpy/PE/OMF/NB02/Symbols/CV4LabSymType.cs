using System;
using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="CV4LABSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct CV4LabSymType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="CV4LABSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="CV4LABSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="CV4LABSYMTYPE.off"/>
        public int off => throw new NotImplementedException();

        /// <inheritdoc cref="CV4LABSYMTYPE.seg"/>
        public ushort seg => throw new NotImplementedException();

        /// <inheritdoc cref="CV4LABSYMTYPE.rtntyp"/>
        public byte rtntyp => throw new NotImplementedException();

        /// <inheritdoc cref="CV4LABSYMTYPE.name"/>
        public SymString name => throw new NotImplementedException();

        internal CV4LabSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
