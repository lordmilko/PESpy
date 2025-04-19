using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FILESTATICSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FileStaticSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FILESTATICSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public CV_uoff32_t modOffset => value->modOffset;

        public CV_LVARFLAGS flags => value->flags;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal FileStaticSym(FILESTATICSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

