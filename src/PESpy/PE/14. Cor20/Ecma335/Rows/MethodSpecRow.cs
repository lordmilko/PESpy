using System;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct MethodSpecRow : IValue, IViewable
    {
        public MethodSpecIndex RowIndex { get; }

        public CodedIndex Method => table.GetMethod(RowIndex);

        public BlobIndex Instantiation => table.GetInstantiation(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object MethodRow => Method.GetRow(table.ModelHeap);

        private readonly MethodSpecTable table;

        internal MethodSpecRow(MethodSpecIndex index, MethodSpecTable table)
        {
            //II.22.29

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public TType[] DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.ModelHeap);
            var reader = Instantiation.GetReader();
            return decoder.DecodeMethodSpecificationSignature(ref reader);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_MethodSpecRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteMethodDefOrRefIndex(nameof(Method), table.MethodOffset, Method);
                    break;

                case 1:
                    structWriter.WriteBlobHeapIndex(nameof(Instantiation), table.InstantiationOffset, Instantiation);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            if (table == null)
                return "<null>";

            using var builder = new ValueStringBuilder();

            builder.Append(Method.GetRow(table.ModelHeap).ToString());
            builder.Append('<');

            GenericParamList genericParams = default;

            if (Method.TableKind == TableKind.MethodDef)
            {
                var methodDef = table.ModelHeap.MethodDefTable[Method];

                genericParams = methodDef.GenericParameters;
            }

            var args = DecodeSignature(StringSignatureTypeProvider.Instance, genericParams);

            for (var i = 0; i < args.Length; i++)
            {
                builder.Append(args[i].ToString());

                if (i < args.Length - 1)
                    builder.Append(", ");
            }

            builder.Append('>');

            return builder.ToString();
        }
    }
}
