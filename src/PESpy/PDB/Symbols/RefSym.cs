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

        //RefSym is the symbol type used by old ST symbols. These symbols have a hidden name after them not accounted for in their lengths
        public FixedUtf8String name => SymType.ReadString(value, ((byte*) value) + reclen + sizeof(ushort));

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        #endregion

        internal RefSym(REFSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

