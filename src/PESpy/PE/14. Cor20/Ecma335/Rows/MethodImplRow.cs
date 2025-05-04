using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Class = {Class}, MethodBody = {MethodBody}, MethodDeclaration = {MethodDeclaration}")]
    public readonly struct MethodImplRow : IValue, IViewable
    {
        public MethodImplIndex RowIndex { get; }

        public int Class => table.GetClass(RowIndex);

        public int MethodBody => table.GetMethodBody(RowIndex);

        public int MethodDeclaration => table.GetMethodDeclaration(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodImplTable table;

        internal MethodImplRow(MethodImplIndex index, MethodImplTable table)
        {
            //II.22.27

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("MethodImpl Row", this, ViewKind.Metadata_MethodImplRow);

            s.WriteSimpleIndex(nameof(Class), Class, TableKind.TypeDef);
            s.WriteMethodDefOrRefIndex(nameof(MethodBody), MethodBody);
            s.WriteMethodDefOrRefIndex(nameof(MethodDeclaration), MethodDeclaration);
        }
    }
}
