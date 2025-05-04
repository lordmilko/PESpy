using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("Module Row", this, ViewKind.Metadata_ModuleRow);

            s.WriteValue(nameof(Generation), Generation);
            s.WriteStringHeapIndex(nameof(Name), Name);
            s.WriteGuidHeapIndex(nameof(Mvid), Mvid);
            s.WriteGuidHeapIndex(nameof(EncId), EncId);
            s.WriteGuidHeapIndex(nameof(EncBaseId), EncBaseId);
        }
    }
}
