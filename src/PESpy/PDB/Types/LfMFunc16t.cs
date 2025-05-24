using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMFunc_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMFunc16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMFunc_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType rvtype => new TypOrEnumType((byte*) value, value->rvtype);

        public TypOrEnumType classtype => new TypOrEnumType((byte*) value, value->classtype);

        public TypOrEnumType thistype => new TypOrEnumType((byte*) value, value->thistype);

        public byte calltype => value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public TypOrEnumType arglist => new TypOrEnumType((byte*) value, value->arglist);

        public int thisadjust => value->thisadjust;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //rvtype
            sizeof(short)  + //classtype
            sizeof(short)  + //thistype
            sizeof(byte)   + //calltype
            1              + //funcattr
            sizeof(short)  + //parmcount
            sizeof(short)  + //arglist
            sizeof(int);     //thisadjust

        internal LfMFunc16t(lfMFunc_16t* value)
        {
            this.value = value;
        }
    }
}
