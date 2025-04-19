using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BPRELSYM32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct BPRelSym3216t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BPRELSYM32_16t* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t off => value->off;
        public CV_typ16_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal BPRelSym3216t(BPRELSYM32_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

