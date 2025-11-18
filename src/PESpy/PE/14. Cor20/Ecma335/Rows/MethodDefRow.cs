using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, ImplFlags = {ImplFlags}, Flags = {Flags}, Name = {ToString(),nq}, Signature = {Signature}, ParamList = {ParamList}")]
    public readonly struct MethodDefRow : IValue, IViewable
    {
        public MethodDefIndex RowIndex { get; }

        public int RVA => table.GetRVA(RowIndex);

        public CorMethodImpl ImplFlags => table.GetImplFlags(RowIndex);

        public CorMethodAttr Flags => table.GetFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public BlobIndex Signature => table.GetSignature(RowIndex);

        public ParamIndex ParamList => table.GetParamList(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodDefTable table;

        internal MethodDefRow(MethodDefIndex index, MethodDefTable table)
        {
            //II.22.26

            RowIndex = index;
            this.table = table;
        }

        public TypeDefRow DeclaringType => table.CompressedModelHeap.GetDeclaringType(RowIndex);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodDefRow, this, ViewKind.Metadata_MethodDefRow, table.RowSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RVA), table.RVAOffset, RVA);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ImplFlags), table.ImplFlagsOffset, ImplFlags, sizeof(short));
                    break;

                case 2:
                    structWriter.WriteField(nameof(Flags), table.FlagsOffset, Flags, sizeof(short));
                    break;

                case 3:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 4:
                    structWriter.WriteBlobHeapIndex(nameof(Signature), table.SignatureOffset, Signature);
                    break;

                case 5:
                    structWriter.WriteSimpleIndex(nameof(ParamList), table.ParamListOffset, (int) ParamList, TableKind.Param);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            using var builder = new Utf8StringBuilder();

            var declaringType = DeclaringType;

            var ns = declaringType.TypeNamespace.GetString();

            if (ns.Length > 0)
            {
                builder.Append(declaringType.TypeNamespace.GetString());
                builder.Append('.');
            }

            builder.Append(declaringType.TypeName.GetString());
            builder.Append('.');
            builder.Append(Name.GetString());

            return builder.ToString();
        }
    }
}
