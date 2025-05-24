using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfProc_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfProc16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfProc_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType rvtype => new TypOrEnumType((byte*) value, value->rvtype);

        public byte calltype => value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public TypOrEnumType arglist => new TypOrEnumType((byte*) value, value->arglist);

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //rvtype
            sizeof(byte)   + //calltype
            1              + //funcattr
            sizeof(short)  + //parmcount
            sizeof(short);   //arglist

        internal LfProc16t(lfProc_16t* value)
        {
            this.value = value;
        }
    }
}
