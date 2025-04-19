using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ManProcSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANPROCSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int pNext => value->pNext;

        public int len => value->len;

        public int DbgStart => value->DbgStart;

        public int DbgEnd => value->DbgEnd;

        public mdToken token => value->token;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public CV_PROCFLAGS flags => value->flags;

        public short retReg => value->retReg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal ManProcSym(MANPROCSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

