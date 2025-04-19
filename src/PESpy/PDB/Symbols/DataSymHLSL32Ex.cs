using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL32_EX"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL32Ex
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL32_EX* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        /// <summary>
        /// Type index
        /// </summary>
        public CV_typ_t typind => value->typind;

        public int regID => value->regID;

        public int dataoff => value->dataoff;

        public int bindSpace => value->bindSpace;

        public int bindSlot => value->bindSlot;

        public short regType => value->regType;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal DataSymHLSL32Ex(DATASYMHLSL32_EX* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

