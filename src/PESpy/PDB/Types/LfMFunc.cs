using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMFunc"/> structure.
    /// </summary>
    public readonly unsafe struct LfMFunc
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMFunc* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType rvtype => new TypOrEnumType((byte*) value, value->rvtype);

        public TypOrEnumType classtype => new TypOrEnumType((byte*) value, value->classtype);

        public TypOrEnumType thistype => new TypOrEnumType((byte*) value, value->thistype);

        public CV_call_e calltype => (CV_call_e) value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public TypOrEnumType arglist => new TypOrEnumType((byte*) value, value->arglist);

        public int thisadjust => value->thisadjust;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //rvtype
            sizeof(int)    + //classtype
            sizeof(int)    + //thistype
            sizeof(byte)   + //calltype
            1              + //funcattr
            sizeof(short)  + //parmcount
            sizeof(int)    + //arglist
            sizeof(int);     //thisadjust

        internal LfMFunc(lfMFunc* value)
        {
            this.value = value;
        }
    }
}
