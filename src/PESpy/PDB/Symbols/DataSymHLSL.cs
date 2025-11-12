using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int regTypeOffset = 8;
        private const int dataslotOffset = 10;
        private const int dataoffOffset = 12;
        private const int texslotOffset = 14;
        private const int sampslotOffset = 16;
        private const int uavslotOffset = 18;
        private const int nameOffset = 20;

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
        public SymString name => SymType.ReadString(value, value->name);

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

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

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal DataSymHLSL(DATASYMHLSL* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYMHLSL, this, ViewKind.DataSymHLSL, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(10, BytesUsed());

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(reclen), reclenOffset, reclen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(rectyp), rectypOffset, rectyp, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 3:
                    structWriter.WriteField(nameof(regType), regTypeOffset, regType);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dataslot), dataslotOffset, dataslot);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dataoff), dataoffOffset, dataoff);
                    break;

                case 6:
                    structWriter.WriteField(nameof(texslot), texslotOffset, texslot);
                    break;

                case 7:
                    structWriter.WriteField(nameof(sampslot), sampslotOffset, sampslot);
                    break;

                case 8:
                    structWriter.WriteField(nameof(uavslot), uavslotOffset, uavslot);
                    break;

                case 9:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 10:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
