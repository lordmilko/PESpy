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

        /// <inheritdoc cref="DATASYMHLSL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYMHLSL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYMHLSL.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYMHLSL.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DATASYMHLSL.dataslot"/>
        public short dataslot => value->dataslot;

        /// <inheritdoc cref="DATASYMHLSL.dataoff"/>
        public short dataoff => value->dataoff;

        /// <inheritdoc cref="DATASYMHLSL.texslot"/>
        public short texslot => value->texslot;

        /// <inheritdoc cref="DATASYMHLSL.sampslot"/>
        public short sampslot => value->sampslot;

        /// <inheritdoc cref="DATASYMHLSL.uavslot"/>
        public short uavslot => value->uavslot;

        /// <inheritdoc cref="DATASYMHLSL.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(short)  + //regType
            sizeof(short)  + //dataslot
            sizeof(short)  + //dataoff
            sizeof(short)  + //texslot
            sizeof(short)  + //sampslot
            sizeof(short);   //uavslot

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

