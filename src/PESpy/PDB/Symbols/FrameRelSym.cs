using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMERELSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FrameRelSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMERELSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t off => value->off;

        public CV_typ_t typind => value->typind;

        public CV_lvar_attr attr => value->attr;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal FrameRelSym(FRAMERELSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

