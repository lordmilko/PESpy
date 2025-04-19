using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PUBSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct PubSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PUBSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_PUBSYMFLAGS pubsymflags => value->pubsymflags;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal PubSym32(PUBSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

