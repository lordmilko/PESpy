using System;
using System.Diagnostics;
using ClrDebug.OMF;
using static PESpy.OldSymType;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="CONSYMTYPE"/> structure.
    /// </summary>
    public readonly unsafe struct ConSymType
    {
        private const int typindOffset16 = 2;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte* value;

        /// <inheritdoc cref="CONSYMTYPE.reclen"/>
        public byte reclen => *value;

        /// <inheritdoc cref="CONSYMTYPE.rectyp"/>
        public OLDSYM rectyp => GetRecTyp(value);

        /// <inheritdoc cref="CONSYMTYPE.typind"/>
        public short typind => *(short*) (value + typindOffset16);

        //Not sure how to generate CONSYMTYPE; I tried and failed
        //to generate one various ways. Need one to figure out how
        //to read Value and Name

        public object Value => throw new NotImplementedException();

        public object Name => throw new NotImplementedException();

        internal ConSymType(byte* value)
        {
            this.value = value;
        }
    }
}
