using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL32_EX"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL32Ex : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int regIDOffset = 8;
        private const int dataoffOffset = 12;
        private const int bindSpaceOffset = 16;
        private const int bindSlotOffset = 20;
        private const int regTypeOffset = 24;
        private const int nameOffset = 26;

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
        public SymString name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(int)    + //regID
            sizeof(int)    + //dataoff
            sizeof(int)    + //bindSpace
            sizeof(int)    + //bindSlot
            sizeof(short);   //regType

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal DataSymHLSL32Ex(DATASYMHLSL32_EX* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYMHLSL32_EX, this, ViewKind.DataSymHLSL32Ex, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(9, BytesUsed());

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
                    structWriter.WriteField(nameof(regID), regIDOffset, regID);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dataoff), dataoffOffset, dataoff);
                    break;

                case 5:
                    structWriter.WriteField(nameof(bindSpace), bindSpaceOffset, bindSpace);
                    break;

                case 6:
                    structWriter.WriteField(nameof(bindSlot), bindSlotOffset, bindSlot);
                    break;

                case 7:
                    structWriter.WriteField(nameof(regType), regTypeOffset, regType);
                    break;

                case 8:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 9:
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
