using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SECTIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct SectionSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SECTIONSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public short isec => value->isec;

        public byte align => value->align;

        public byte bReserved => value->bReserved;

        public int rva => value->rva;

        public int cb => value->cb;

        public int characteristics => value->characteristics;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal SectionSym(SECTIONSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

