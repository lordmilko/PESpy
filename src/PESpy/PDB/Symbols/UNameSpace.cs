using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UNAMESPACE"/> structure.
    /// </summary>
    public readonly unsafe struct UNameSpace
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UNAMESPACE* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal UNameSpace(UNAMESPACE* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

