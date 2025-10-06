using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("Generation = {Generation}, Name = {Name.ToString(),nq}, Mvid = {Mvid}, EncId = {EncId}, EncBaseId = {EncBaseId}")]
    public readonly struct ModuleRow : IValue, IViewable
    {
        public ModuleIndex RowIndex { get; }

        public short Generation => table.GetGeneration(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public GuidIndex Mvid => table.GetMvid(RowIndex);

        public GuidIndex EncId => table.GetEncId(RowIndex);

        public GuidIndex EncBaseId => table.GetEncBaseId(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ModuleTable table;

        internal ModuleRow(ModuleIndex index, ModuleTable table)
        {
            //II.22.30

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.ModuleRow, this, ViewKind.Metadata_ModuleRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(Generation), Generation);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteGuidHeapIndex(nameof(Mvid), Mvid);
            s.WriteGuidHeapIndex(nameof(EncId), EncId);
            s.WriteGuidHeapIndex(nameof(EncBaseId), EncBaseId);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
