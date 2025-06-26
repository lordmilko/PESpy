using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
            writer.NewStruct("TypeRef Row", this, ViewKind.Metadata_TypeRefRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteResolutionScopeIndex(nameof(ResolutionScope), (int) ResolutionScope);
            s.WriteStringHeapIndex(nameof(TypeName), TypeName);
            s.WriteStringHeapIndex(nameof(TypeNamespace), TypeNamespace);

            return s.ToArray();
        }
    }
}
