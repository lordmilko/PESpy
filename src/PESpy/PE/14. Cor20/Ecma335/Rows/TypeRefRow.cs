using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("ResolutionScope = {ResolutionScope}, TypeName = {TypeName.ToString(),nq}, TypeNamespace = {TypeNamespace.ToString(),nq}")]
    public readonly struct TypeRefRow : IValue, IViewable
    {
        public TypeRefIndex RowIndex { get; }

        public Index ResolutionScope => table.GetResolutionScope(RowIndex);

        public StringIndex TypeName => table.GetTypeName(RowIndex);

        public StringIndex TypeNamespace => table.GetTypeNamespace(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

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
            writer.NewStruct(Strings.TypeRefRow, this, ViewKind.Metadata_TypeRefRow, table.RowSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteResolutionScopeIndex(nameof(ResolutionScope), table.ResolutionScopeOffset, (int) ResolutionScope);
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
    }
}
