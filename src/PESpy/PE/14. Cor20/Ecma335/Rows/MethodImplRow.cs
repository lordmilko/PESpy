using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Class = {Class}, MethodBody = {MethodBody}, MethodDeclaration = {MethodDeclaration}")]
    public readonly struct MethodImplRow : IValue, IViewable
    {
        public MethodImplIndex RowIndex { get; }

        public TypeDefIndex Class => table.GetClass(RowIndex);

        public Index MethodBody => table.GetMethodBody(RowIndex);

        public Index MethodDeclaration => table.GetMethodDeclaration(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly MethodImplTable table;

        internal MethodImplRow(MethodImplIndex index, MethodImplTable table)
        {
            //II.22.27

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MethodImplRow, this, ViewKind.Metadata_MethodImplRow, table.RowSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Class), table.ClassOffset, (int) Class, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteMethodDefOrRefIndex(nameof(MethodBody), table.MethodBodyOffset, (int) MethodBody);
                    break;

                case 2:
                    structWriter.WriteMethodDefOrRefIndex(nameof(MethodDeclaration), table.MethodDeclarationOffset, (int) MethodDeclaration);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
