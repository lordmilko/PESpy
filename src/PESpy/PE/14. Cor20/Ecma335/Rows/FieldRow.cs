using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct FieldRow : IValue, IViewable
    {
        private string DebuggerDisplay() => $"{DecodeSignature(StringSignatureTypeProvider.Instance, default)} {DeclaringType}.{Name.GetString()}";

        public FieldIndex RowIndex { get; }

        public CorFieldAttr Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

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

        private readonly FieldTable table;

        internal FieldRow(FieldIndex index, FieldTable table)
        {
            //II.22.15

            RowIndex = index;
            this.table = table;
        }

        public TType DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.ModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeFieldSignature(ref reader);
        }

        public TypeDefRow? DeclaringType => table.ModelHeap.GetDeclaringType(RowIndex);

        public ConstantIndex DefaultValue => table.ModelHeap.ConstantTable.FindConstant(HasConstantTag.CreateIndex(RowIndex.RowId, TableKind.Field));

        /* II.22.18
         * 
         * Conceptually, each row in the FieldRVA table is an extension to exactly one row in the Field table, and
         * records the RVA (Relative Virtual Address) within the image file at which this field’s initial value is stored.
         * 
         * A row in the FieldRVA table is created for each static parent field that has specified the optional data
         * label §II.16). The RVA column is the relative virtual address of the data in the PE file (§II.16.3).
         */
        public int RelativeVirtualAddress
        {
            get
            {
                var fieldRvaRowIndex = table.ModelHeap.FieldRvaTable.FindFieldRvaRowId(RowIndex.RowId);

                if (fieldRvaRowIndex.RowId == 0)
                    return 0;

                return table.ModelHeap.FieldRvaTable.GetRVA(fieldRvaRowIndex);
            }
        }

        public BlobIndex MarshallingDescriptor
        {
            get
            {
                var fieldMarshalTable = table.ModelHeap.FieldMarshalTable;

                if (fieldMarshalTable == null)
                    return default;

                var marshalRowIndex = fieldMarshalTable.FindFieldMarshalRowId(HasFieldMarshalTag.CreateIndex(RowIndex.RowId, TableKind.Field));

                if (marshalRowIndex.RowId == 0)
                    return default;

                return fieldMarshalTable.GetNativeType(marshalRowIndex);
            }
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_FieldRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteBlobHeapIndex(nameof(Signature), table.SignatureOffset, Signature);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
