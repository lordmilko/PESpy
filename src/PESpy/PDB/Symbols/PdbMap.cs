using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PDBMAP"/> structure.
    /// </summary>
    public readonly unsafe struct PdbMap
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PDBMAP* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal PdbMap(PDBMAP* value)
        {
            this.value = value;
            Debug.Assert(false, "Read destination PDB FileName");
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

