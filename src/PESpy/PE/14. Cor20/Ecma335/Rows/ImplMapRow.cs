using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("MappingFlags = {MappingFlags}, MemberForwarded = {MemberForwarded}, ImportName = {ImportName.ToString(),nq}, ImportScope = {ImportScope}")]
    public readonly struct ImplMapRow : IValue, IViewable
    {
        public ImplMapIndex RowIndex { get; }

        public CorPinvokeMap MappingFlags => table.GetMappingFlags(RowIndex);

        public Index MemberForwarded => table.GetMemberForwarded(RowIndex);

        public StringIndex ImportName => table.GetImportName(RowIndex);

        public ModuleRefIndex ImportScope => table.GetImportScope(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly ImplMapTable table;

        internal ImplMapRow(ImplMapIndex index, ImplMapTable table)
        {
            //II.22.22

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("ImplMap Row", this, ViewKind.Metadata_ImplMapRow);

            s.WriteValue(nameof(MappingFlags), MappingFlags, sizeof(short));
            s.WriteMemberForwardedIndex(nameof(MemberForwarded), (int) MemberForwarded);
            s.WriteStringHeapIndex(nameof(ImportName), ImportName);
            s.WriteSimpleIndex(nameof(ImportScope), (int) ImportScope, TableKind.ModuleRef);
        }
    }
}
