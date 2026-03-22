using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("[{Kind}] {ToString(),nq}")]
    public readonly struct MemberRefRow : IValue, IViewable
    {
        public MemberRefIndex RowIndex { get; }

        public CodedIndex Class => table.GetClass(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public CorCallingConvention Kind
        {
            get
            {
                var reader = Signature.GetReader();
                var callingConv = (CorCallingConvention) reader.ReadByte();
                return callingConv;
            }
        }

        public object ClassRow => Class.GetRow(table.CompressedModelHeap);

        private readonly MemberRefTable table;

        internal MemberRefRow(MemberRefIndex index, MemberRefTable table)
        {
            //II.22.25

            RowIndex = index;
            this.table = table;
        }

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public TType DecodeFieldSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.CompressedModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeFieldSignature(ref reader);
        }

        public MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
        {
            var decoder = new SignatureDecoder<TType, TGenericContext>(provider, genericContext, table.CompressedModelHeap);
            var reader = Signature.GetReader();
            return decoder.DecodeMethodSignature(ref reader);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MemberRefRow, this, ViewKind.Metadata_MemberRefRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteMemberRefParentIndex(nameof(Class), table.ClassOffset, Class);
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

        public override string ToString() => $"{ClassRow}.{Name.GetString()}";
    }
}
