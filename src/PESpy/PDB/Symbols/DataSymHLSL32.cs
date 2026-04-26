using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYMHLSL32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSymHLSL32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int typindOffset = 4;
        private const int dataslotOffset = 8;
        private const int dataoffOffset = 12;
        private const int texslotOffset = 16;
        private const int sampslotOffset = 20;
        private const int uavslotOffset = 24;
        private const int regTypeOffset = 28;
        private const int nameOffset = 30;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYMHLSL32* value;

        public static implicit operator SymType(DataSymHLSL32 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DATASYMHLSL32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYMHLSL32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYMHLSL32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYMHLSL32.dataslot"/>
        public int dataslot => value->dataslot;

        /// <inheritdoc cref="DATASYMHLSL32.dataoff"/>
        public int dataoff => value->dataoff;

        /// <inheritdoc cref="DATASYMHLSL32.texslot"/>
        public int texslot => value->texslot;

        /// <inheritdoc cref="DATASYMHLSL32.sampslot"/>
        public int sampslot => value->sampslot;

        /// <inheritdoc cref="DATASYMHLSL32.uavslot"/>
        public int uavslot => value->uavslot;

        /// <inheritdoc cref="DATASYMHLSL32.regType"/>
        public short regType => value->regType;

        /// <inheritdoc cref="DATASYMHLSL32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(int)    + //dataslot
            sizeof(int)    + //dataoff
            sizeof(int)    + //texslot
            sizeof(int)    + //sampslot
            sizeof(int)    + //uavslot
            sizeof(short);   //regType

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal DataSymHLSL32(DATASYMHLSL32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.DataSymHLSL32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(dataslot), dataslotOffset, dataslot);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dataoff), dataoffOffset, dataoff);
                    break;

                case 5:
                    structWriter.WriteField(nameof(texslot), texslotOffset, texslot);
                    break;

                case 6:
                    structWriter.WriteField(nameof(sampslot), sampslotOffset, sampslot);
                    break;

                case 7:
                    structWriter.WriteField(nameof(uavslot), uavslotOffset, uavslot);
                    break;

                case 8:
                    structWriter.WriteField(nameof(regType), regTypeOffset, regType);
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
