using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("MoveNextMethod = {MoveNextMethod}, KickoffMethod = {KickoffMethod}")]
    public readonly struct StateMachineMethodRow : IValue, IViewable
    {
        public StateMachineMethodIndex RowIndex { get; }

        public MethodDefIndex MoveNextMethod => table.GetMoveNextMethod(RowIndex);

        public MethodDefIndex KickoffMethod => table.GetKickoffMethod(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        private readonly StateMachineMethodTable table;

        internal StateMachineMethodRow(StateMachineMethodIndex index, StateMachineMethodTable table)
        {
            //https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#statemachinemethod-table-0x36

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.StateMachineMethodRow, this, ViewKind.PortablePdb_StateMachineMethodRow, table.RowSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateMetadataRow(parent);

            s.WriteValue(nameof(MoveNextMethod), (int) MoveNextMethod);
            s.WriteValue(nameof(KickoffMethod), (int) KickoffMethod);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
