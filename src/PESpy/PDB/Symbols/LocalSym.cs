using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LOCALSYM"/> structure.
    /// </summary>
    public readonly unsafe struct LocalSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LOCALSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public CV_LVARFLAGS flags => value->flags;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal LocalSym(LOCALSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

