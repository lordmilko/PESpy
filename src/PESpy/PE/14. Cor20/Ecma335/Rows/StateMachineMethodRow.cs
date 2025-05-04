using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.Ecma335
{
    [DebuggerDisplay("MoveNextMethod = {MoveNextMethod}, KickoffMethod = {KickoffMethod}")]
    public readonly struct StateMachineMethodRow : IValue, IViewable
    {
        public StateMachineMethodIndex RowIndex { get; }

        public int MoveNextMethod => table.GetMoveNextMethod(RowIndex);

        public int KickoffMethod => table.GetKickoffMethod(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly StateMachineMethodTable table;

        internal StateMachineMethodRow(StateMachineMethodIndex index, StateMachineMethodTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#statemachinemethod-table-0x36

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateMetadataRow("StateMachineMethod Row", this, ViewKind.PortablePdb_StateMachineMethodRow);

            s.WriteValue(nameof(MoveNextMethod), MoveNextMethod);
            s.WriteValue(nameof(KickoffMethod), KickoffMethod);
        }
    }
}
