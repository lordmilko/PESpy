using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct RefSym2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFSYM2* value;

        /// <inheritdoc cref="REFSYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFSYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFSYM2.sumName"/>
        public int sumName => value->sumName;

        /// <inheritdoc cref="REFSYM2.ibSym"/>
        public int ibSym => value->ibSym;

        /// <inheritdoc cref="REFSYM2.imod"/>
        public short imod => value->imod;

        /// <inheritdoc cref="REFSYM2.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //sumName
            sizeof(int)    + //ibSym
            sizeof(short);   //imod

        internal RefSym2(REFSYM2* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

