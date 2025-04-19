using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public int dataslot => value->dataslot;

        public int dataoff => value->dataoff;

        public int texslot => value->texslot;

        public int sampslot => value->sampslot;

        public int uavslot => value->uavslot;

        public short regType => value->regType;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal DataSymHLSL32(DATASYMHLSL32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

