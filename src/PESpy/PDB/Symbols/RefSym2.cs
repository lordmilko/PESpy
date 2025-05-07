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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int sumName => value->sumName;

        public int ibSym => value->ibSym;

        public short imod => value->imod;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        #endregion

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

