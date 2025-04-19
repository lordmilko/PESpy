using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LOCALDPCGROUPSHAREDSYM"/> structure.
    /// </summary>
    public readonly unsafe struct LocalDPCGroupSharedSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LOCALDPCGROUPSHAREDSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public CV_LVARFLAGS flags => value->flags;

        public short dataslot => value->dataslot;

        public short dataoff => value->dataoff;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal LocalDPCGroupSharedSym(LOCALDPCGROUPSHAREDSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

