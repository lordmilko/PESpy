using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Parent = {ParentRow}, Imports = {Imports}")]
    public readonly struct ImportScopeRow : IValue, IViewable
    {
        public ImportScopeIndex RowIndex { get; }

        public ImportScopeIndex Parent => table.GetParent(RowIndex);

        public BlobIndex Imports => table.GetImports(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public ImportScopeRow? ParentRow
        {
            get
            {
                var parent = Parent;

                if (parent.IsNil)
                    return null;

                return table[parent];
            }
        }

        private readonly ImportScopeTable table;

        internal ImportScopeRow(ImportScopeIndex index, ImportScopeTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#importscope-table-0x35

            RowIndex = index;
            this.table = table;
        }

        //System.Reflection.Metadata has an "Imports" collection that parses the imports blob

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ImportScopeRow, this, ViewKind.PortablePdb_ImportScopeRow, table.RowSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Parent), table.ParentOffset, (int) Parent);
                    break;

                case 1:
                    structWriter.WriteBlobHeapIndex(nameof(Imports), table.ImportsOffset, Imports);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
