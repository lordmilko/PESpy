using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public short regType => value->regType;

        public short dataslot => value->dataslot;

        public short dataoff => value->dataoff;

        public short texslot => value->texslot;

        public short sampslot => value->sampslot;

        public short uavslot => value->uavslot;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal DataSymHLSL(DATASYMHLSL* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

