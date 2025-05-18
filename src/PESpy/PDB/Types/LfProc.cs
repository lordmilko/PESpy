using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfProc"/> structure.
    /// </summary>
    public readonly unsafe struct LfProc
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfProc* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType rvtype => new TypOrEnumType((byte*) value, value->rvtype);

        public CV_call_e calltype => (CV_call_e) value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public TypOrEnumType arglist => new TypOrEnumType((byte*) value, value->arglist);

        internal LfProc(lfProc* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return $"{rvtype} <fn>{arglist}";
        }
    }
}
