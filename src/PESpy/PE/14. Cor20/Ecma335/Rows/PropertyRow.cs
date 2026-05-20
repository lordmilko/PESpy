using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct PropertyRow : IValue, IViewable
    {
        private string DebuggerDisplay()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(DeclaringType.ToString());
            builder.Append(".");
            builder.Append(Name.GetString().AsSpan());

            var accessors = Accessors;

            if (!accessors.Getter.IsNil)
            {
                if (!accessors.Setter.IsNil)
                    builder.Append(" { get; set; }");
                else
                    builder.Append(" { get; }");
            }
            else
            {
                if (!accessors.Setter.IsNil)
                    builder.Append(" { set; }");
            }

            return builder.ToString();
        }

        public PropertyIndex RowIndex { get; }

        public CorPropertyAttr Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Type => table.GetType(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public ConstantRow? DefaultValueRow
        {
            get
            {
                var defaultValue = DefaultValue;

                if (defaultValue.IsNil)
                    return null;

                var constantTable = table.ModelHeap.ConstantTable;

                if (constantTable == null)
                    return null;

                return constantTable[DefaultValue];
            }
        }

        private readonly PropertyTable table;

        internal PropertyRow(PropertyIndex index, PropertyTable table)
        {
            //II.22.34

            RowIndex = index;
            this.table = table;
        }

        public MethodSignature<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.ModelHeap);
            var reader = Type.GetReader();
            return decoder.DecodeMethodSignature(ref reader);
        }

        public ConstantIndex DefaultValue => table.ModelHeap.ConstantTable.FindConstant(HasConstantTag.CreateIndex(RowIndex.RowId, TableKind.Property));

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public TypeDefRow? DeclaringType => table.ModelHeap.GetDeclaringType(RowIndex);

        public PropertyAccessors Accessors
        {
            get
            {
                ushort methodCount = 0;

                var methodSemanticsTable = table.ModelHeap.MethodSemanticsTable;

                var firstRowId = methodSemanticsTable.FindSemanticMethods(
                    HasSemanticsTag.CreateIndex(RowIndex.RowId, TableKind.Property),
                    ref methodCount
                );

                var getter = 0;
                var setter = 0;

                using var others = new ValueList<MethodDefIndex>();

                for (var i = 0; i < methodCount; i++)
                {
                    var rowId = (MethodSemanticsIndex) (firstRowId + i);

                    switch (methodSemanticsTable.GetSemantics(rowId))
                    {
                        case CorMethodSemanticsAttr.msGetter:
                            getter = methodSemanticsTable.GetMethod(rowId).RowId;
                            break;

                        case CorMethodSemanticsAttr.msSetter:
                            setter = methodSemanticsTable.GetMethod(rowId).RowId;
                            break;

                        default:
                            others.Add(methodSemanticsTable.GetMethod(rowId));
                            break;
                    }
                }

                return new PropertyAccessors(getter, setter, others.ToArray());
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_PropertyRow, table.RowSize);

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
                    structWriter.WriteBlobHeapIndex(nameof(Type), table.TypeOffset, Type);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
