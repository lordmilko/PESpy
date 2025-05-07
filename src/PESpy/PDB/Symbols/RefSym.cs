using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFSYM"/> structure.
    /// </summary>
    public readonly unsafe struct RefSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int sumName => value->sumName;

        public int ibSym => value->ibSym;

        public short imod => value->imod;

        public short usFill => value->usFill;

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        #endregion

        internal RefSym(REFSYM* value)
        {
            this.value = value;
        }
    }
}

