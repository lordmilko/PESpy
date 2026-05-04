using System;
using System.Diagnostics;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct EventRow : IValue, IViewable
    {
        private string DebuggerDisplay() => $"{DeclaringType}.{Name.GetString()}";

        public EventIndex RowIndex { get; }

        public CorEventAttr EventFlags => table.GetEventFlags(RowIndex);

        public StringIndex Name => table.GetName(RowIndex);

        public CodedIndex EventType => table.GetEventType(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object EventTypeRow => EventType.GetRow(table.CompressedModelHeap);

        private readonly EventTable table;

        internal EventRow(EventIndex index, EventTable table)
        {
            //II.22.13

            RowIndex = index;
            this.table = table;
        }

        public TypeDefRow? DeclaringType => table.CompressedModelHeap.GetDeclaringType(RowIndex);

        public CustomAttributeList CustomAttributes => table.GetCustomAttributes(RowIndex);

        public EventAccessors Accessors
        {
            get
            {
                ushort methodCount = 0;

                var methodSemanticsTable = table.CompressedModelHeap.MethodSemanticsTable;

                var firstRowId = methodSemanticsTable.FindSemanticMethods(
                    HasSemanticsTag.CreateIndex(RowIndex.RowId, TableKind.Event),
                    ref methodCount
                );

                var adder = 0;
                var remover = 0;
                var fire = 0;

                using var others = new PooledList<MethodDefIndex>();

                for (var i = 0; i < methodCount; i++)
                {
                    var rowId = (MethodSemanticsIndex) (firstRowId + i);

                    switch (methodSemanticsTable.GetSemantics(rowId))
                    {
                        case CorMethodSemanticsAttr.msAddOn: //adder
                            adder = methodSemanticsTable.GetMethod(rowId).RowId;
                            break;

                        case CorMethodSemanticsAttr.msRemoveOn: //remover
                            remover = methodSemanticsTable.GetMethod(rowId).RowId;
                            break;

                        case CorMethodSemanticsAttr.msFire: //raiser
                            fire = methodSemanticsTable.GetMethod(rowId).RowId;
                            break;

                        default:
                            others.Add(methodSemanticsTable.GetMethod(rowId));
                            break;
                    }
                }

                return new EventAccessors(adder, remover, fire, others.ToArray());
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_EventRow, table.RowSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(EventFlags), table.EventFlagsOffset, EventFlags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteTypeDefOrRefIndex(nameof(EventType), table.EventTypeOffset, EventType);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString() => Name.GetString().ToString();
    }
}
