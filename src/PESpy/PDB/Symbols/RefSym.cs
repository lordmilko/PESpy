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

        /// <inheritdoc cref="REFSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFSYM.sumName"/>
        public int sumName => value->sumName;

        /// <inheritdoc cref="REFSYM.ibSym"/>
        public int ibSym => value->ibSym;

        /// <inheritdoc cref="REFSYM.imod"/>
        public short imod => value->imod;

        /// <inheritdoc cref="REFSYM.usFill"/>
        public short usFill => value->usFill;

        //RefSym is the symbol type used by old ST symbols. These symbols have a hidden name after them not accounted for in their lengths
        public FixedUtf8String name => SymType.ReadString(value, ((byte*) value) + reclen + sizeof(ushort));

        #region PESpy

        public SymType Symbol => SymType.GetSymbol(value, imod, ibSym);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //sumName
            sizeof(int)    + //ibSym
            sizeof(short)  + //imod
            sizeof(short);   //usFill

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

