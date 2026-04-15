using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Type = {Type}, Parent = {ParentRow.ToString()}, Value = {ClrValue}")]
    public readonly struct ConstantRow : IValue, IViewable
    {
        public ConstantIndex RowIndex { get; }

        public CorElementType Type => table.GetType(RowIndex);

        public byte Padding => table.GetPadding(RowIndex);

        public CodedIndex Parent => table.GetParent(RowIndex);

        public BlobIndex Value => table.GetValue(RowIndex);

        public unsafe object ClrValue
        {
            get
            {
                var bytes = Value.GetBlob().Value;
                var pValue = (byte*) bytes;

                return Type switch
                {
                    //The only possible types are listed in II.22.9
                    CorElementType.Boolean => *(bool*) pValue,
                    CorElementType.Char => *(char*) pValue,
                    CorElementType.I1 => *(sbyte*) pValue,
                    CorElementType.I2 => *(short*) pValue,
                    CorElementType.I4 => *(int*) pValue,
                    CorElementType.I8 => *(long*) pValue,
                    CorElementType.U1 => *(byte*) pValue,
                    CorElementType.U2 => *(ushort*) pValue,
                    CorElementType.U4 => *(uint*) pValue,
                    CorElementType.U8 => *(ulong*) pValue,
                    CorElementType.R4 => *(float*) pValue,
                    CorElementType.R8 => *(double*) pValue,
                    CorElementType.String => new FixedUtf16String((char*) pValue, bytes.Length / 2).ToString(),
                    CorElementType.Class => null, //The bytes should be 0 to indicate null
                };
            }
        }

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ParentRow => Parent.GetRow(table.CompressedModelHeap);

        private readonly ConstantTable table;

        internal ConstantRow(ConstantIndex index, ConstantTable table)
        {
            //II.22.9

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ConstantRow, this, ViewKind.Metadata_ConstantRow, table.RowSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Type), table.TypeOffset, Type, sizeof(byte));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Padding), table.PaddingOffset, Padding);
                    break;

                case 2:
                    structWriter.WriteHasConstantIndex(nameof(Parent), table.ParentOffset, Parent);
                    break;

                case 3:
                    structWriter.WriteBlobHeapIndex(nameof(Value), table.ValueOffset, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return ClrValue.ToString();
        }
    }
}
