using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Flags = {Flags}, Sequence = {Sequence}, Name = {Name.ToString(),nq}")]
    public readonly struct ParamRow : IValue, IViewable
    {
        public ParamIndex RowIndex { get; }

        public CorParamAttr Flags => table.GetFlags(RowIndex);

        public short Sequence => table.GetSequence(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public ConstantRow? DefaultValueRow
        {
            get
            {
                var defaultValue = DefaultValue;

                if (defaultValue.IsNil)
                    return null;

                return table.ModelHeap.ConstantTable[DefaultValue];
            }
        }

        private readonly ParamTable table;

        internal ParamRow(ParamIndex index, ParamTable table)
        {
            //II.22.33

            RowIndex = index;
            this.table = table;
        }

        public ConstantIndex DefaultValue => table.ModelHeap.ConstantTable.FindConstant(HasConstantTag.CreateIndex(RowIndex.RowId, TableKind.Param));

        public BlobIndex MarshallingDescriptor
        {
            get
            {
                var fieldMarshalTable = table.ModelHeap.FieldMarshalTable;

                if (fieldMarshalTable == null)
                    return default;

                var marshalIndex = fieldMarshalTable.FindFieldMarshalRowId(HasFieldMarshalTag.CreateIndex(RowIndex.RowId, TableKind.Param));

                if (marshalIndex.RowId == 0)
                    return default;

                return fieldMarshalTable.GetNativeType(marshalIndex);
            }
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_ParamRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Sequence), table.SequenceOffset, Sequence);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
