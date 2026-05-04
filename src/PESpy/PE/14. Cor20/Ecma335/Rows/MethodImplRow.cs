using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Class = {ClassRow}, MethodBody = {MethodBodyRow}, MethodDeclaration = {MethodDeclarationRow}")]
    public readonly struct MethodImplRow : IValue, IViewable
    {
        public MethodImplIndex RowIndex { get; }

        public TypeDefIndex Class => table.GetClass(RowIndex);

        public CodedIndex MethodBody => table.GetMethodBody(RowIndex);

        public CodedIndex MethodDeclaration => table.GetMethodDeclaration(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public TypeDefRow ClassRow => table.CompressedModelHeap.TypeDefTable[Class];

        public object MethodBodyRow => MethodBody.GetRow(table.CompressedModelHeap);

        public object MethodDeclarationRow => MethodDeclaration.GetRow(table.CompressedModelHeap);

        private readonly MethodImplTable table;

        internal MethodImplRow(MethodImplIndex index, MethodImplTable table)
        {
            //II.22.27

            RowIndex = index;
            this.table = table;
        }

        //System.Reflection.Metadata contains a GetCustomAttributes method on MethodImplementation,
        //however this seems suspicious: MethodImpl is not a supported table kind for the
        //HasCustomAttributeTag coded index, and it seems like SRM just returns a coded index of 0
        //for this. I feel like it was a mistake adding it?

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_MethodImplRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteSimpleIndex(nameof(Class), table.ClassOffset, (int) Class, TableKind.TypeDef);
                    break;

                case 1:
                    structWriter.WriteMethodDefOrRefIndex(nameof(MethodBody), table.MethodBodyOffset, MethodBody);
                    break;

                case 2:
                    structWriter.WriteMethodDefOrRefIndex(nameof(MethodDeclaration), table.MethodDeclarationOffset, MethodDeclaration);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
