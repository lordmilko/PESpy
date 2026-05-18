using System;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct TypeRefRow : IValue, IViewable
    {
        public TypeRefIndex RowIndex { get; }

        public CodedIndex ResolutionScope => table.GetResolutionScope(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object ResolutionScopeRow => ResolutionScope.GetRow(table.ModelHeap);

        private readonly TypeRefTable table;

        internal TypeRefRow(TypeRefIndex index, TypeRefTable table)
        {
            //II.22.38

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_TypeRefRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteResolutionScopeIndex(nameof(ResolutionScope), table.ResolutionScopeOffset, ResolutionScope);
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(TypeName), table.TypeNameOffset, TypeName);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(TypeNamespace), table.TypeNamespaceOffset, TypeNamespace);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => ModelHeap.FormatType(TypeNamespace, TypeName);
    }
}
