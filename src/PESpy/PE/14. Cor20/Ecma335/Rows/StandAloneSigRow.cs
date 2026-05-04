using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct StandAloneSigRow : IValue, IViewable
    {
        private string DebuggerDisplay()
        {
            using var builder = new ValueStringBuilder();

            builder.Append("Kind = ");
            builder.Append(Kind.ToString());

            if (Kind == CorCallingConvention.LOCAL_SIG)
            {
                builder.Append(", Types = ");

                var types = DecodeLocalSignature(StringSignatureTypeProvider.Instance, default);

                for (var i = 0; i < types.Length; i++)
                {
                    builder.Append(types[i]);

                    if (i < types.Length - 1)
                        builder.Append(", ");
                }
            }

            return builder.ToString();
        }

        public StandAloneSigIndex RowIndex { get; }

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        public CorCallingConvention Kind
        {
            get
            {
                var reader = Signature.GetReader();
                var callingConv = (CorCallingConvention) reader.ReadByte();
                return callingConv;
            }
        }

        private readonly StandAloneSigTable table;

        internal StandAloneSigRow(StandAloneSigIndex index, StandAloneSigTable table)
        {
            //II.22.36

            RowIndex = index;
            this.table = table;
        }

        public MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.CompressedModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeMethodSignature(ref reader);
        }

        public TType[] DecodeLocalSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.CompressedModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeLocalSignature(ref reader);
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_StandAloneSigRow, table.RowSize);

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
    }
}
