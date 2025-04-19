using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="EXPORTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ExportSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly EXPORTSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public short ordinal => value->ordinal;

        public bool fConstant => value->fConstant;

        public bool fData => value->fData;

        public bool fPrivate => value->fPrivate;

        public bool fNoName => value->fNoName;

        public bool fOrdinal => value->fOrdinal;

        public bool fForwarder => value->fForwarder;

        public short reserved => value->reserved;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal ExportSym(EXPORTSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

