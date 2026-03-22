using System;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct TypeSpecRow : IValue, IViewable
    {
        public TypeSpecIndex RowIndex { get; }

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly TypeSpecTable table;

        internal TypeSpecRow(TypeSpecIndex index, TypeSpecTable table)
        {
            //II.22.39

            RowIndex = index;
            this.table = table;
        }

        //Extensions
        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public TType DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.CompressedModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeType(ref reader);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TypeSpecRow, this, ViewKind.Metadata_TypeSpecRow, table.RowSize);

        int IViewable.NumChildren() => 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBlobHeapIndex(nameof(Signature), table.SignatureOffset, Signature);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            //Ideally we'd like to get some generic parameters from our parent and pass them in,
            //but we don't have access to our parent from within this method!
            return DecodeSignature(StringSignatureTypeProvider.Instance, default);
        }
    }
}
