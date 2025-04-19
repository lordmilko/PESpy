using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OBJNAMESYM"/> structure.
    /// </summary>
    public readonly unsafe struct ObjNameSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OBJNAMESYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int signature => value->signature;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal ObjNameSym(OBJNAMESYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

