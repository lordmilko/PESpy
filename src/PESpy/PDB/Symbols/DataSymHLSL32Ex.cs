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

        /// <inheritdoc cref="DATASYMHLSL32_EX.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYMHLSL32_EX.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <summary>
        /// Type index
        /// </summary>
        /// <inheritdoc cref="DATASYMHLSL32_EX.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYMHLSL32_EX.regID"/>
        public int regID => value->regID;

        /// <inheritdoc cref="DATASYMHLSL32_EX.dataoff"/>
        public int dataoff => value->dataoff;

        /// <inheritdoc cref="DATASYMHLSL32_EX.bindSpace"/>
        public int bindSpace => value->bindSpace;

        /// <inheritdoc cref="DATASYMHLSL32_EX.bindSlot"/>
        public int bindSlot => value->bindSlot;

        /// <inheritdoc cref="DATASYMHLSL32_EX.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DATASYMHLSL32_EX.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(int)    + //regID
            sizeof(int)    + //dataoff
            sizeof(int)    + //bindSpace
            sizeof(int)    + //bindSlot
            sizeof(short);   //regType

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

