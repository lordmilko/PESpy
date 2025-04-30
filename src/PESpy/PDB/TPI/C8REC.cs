using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    //Used in PDB1
    public struct C8REC
    {
        public ushort hash;
        public TYPTYPE type;
    }

    //Managed representation of C8REC
    public unsafe struct C8Rec
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly C8REC* value;

        public ushort hash => value->hash;

        public TypType type => &value->type;

        public C8Rec(C8REC* value)
        {
            this.value = value;
        }

        public static implicit operator C8Rec(C8REC* value) => new C8Rec(value);

        public override string ToString()
        {
            TypType type = &value->type;

            return type.ToString();
        }
    }
}
