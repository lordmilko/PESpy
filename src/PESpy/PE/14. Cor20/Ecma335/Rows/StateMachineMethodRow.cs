using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("MoveNextMethod = {MoveNextMethodRow}, KickoffMethod = {KickoffMethodRow}")]
    public readonly struct StateMachineMethodRow : IValue, IViewable
    {
        public StateMachineMethodIndex RowIndex { get; }

        public MethodDefIndex MoveNextMethod => table.GetMoveNextMethod(RowIndex);

        public MethodDefIndex KickoffMethod => table.GetKickoffMethod(RowIndex);

        public int Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public MethodDefRow MoveNextMethodRow => table.CompressedModelHeap.MethodDefTable[MoveNextMethod];

        public MethodDefRow KickoffMethodRow => table.CompressedModelHeap.MethodDefTable[KickoffMethod];

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

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(MoveNextMethod), table.MoveNextMethodOffset, (int) MoveNextMethod);
                    break;

                case 1:
                    structWriter.WriteField(nameof(KickoffMethod), table.KickoffMethodOffset, (int) KickoffMethod);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
