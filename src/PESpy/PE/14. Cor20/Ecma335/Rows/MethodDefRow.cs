using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("RVA = 0x{RVA.ToString(\"X\"),nq}, ImplFlags = {ImplFlags}, Flags = {Flags}, Name = {Name.ToString(),nq}, Signature = {Signature}, ParamList = {ParamList}")]
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodDefRow, this, ViewKind.Metadata_MethodDefRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(RVA), RVA);
            s.WriteValue(nameof(ImplFlags), ImplFlags, sizeof(short));
            s.WriteValue(nameof(Flags), Flags, sizeof(short));
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteBlobHeapIndex(nameof(Signature), Signature);
            s.WriteSimpleIndex(nameof(ParamList), (int) ParamList, TableKind.Param);

            return s.ToArray();
        }
    }
}
