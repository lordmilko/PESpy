using System;
using ClrDebug;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly struct ImplMapRow : IValue, IViewable
    {
        public ImplMapIndex RowIndex { get; }

        public CorPinvokeMap MappingFlags => table.GetMappingFlags(RowIndex);

        public CodedIndex MemberForwarded => table.GetMemberForwarded(RowIndex);

        public StringIndex ImportName => table.GetImportName(RowIndex);

        public ModuleRefIndex ImportScope => table.GetImportScope(RowIndex);

        public long Offset => table.GetRowOffset(RowIndex);

        //Extensions
        public object MemberForwardedRow => MemberForwarded.GetRow(table.CompressedModelHeap);

        public ModuleRefRow ImportScopeRow => table.CompressedModelHeap.ModuleRefTable[ImportScope];

        private readonly ImplMapTable table;

        internal ImplMapRow(ImplMapIndex index, ImplMapTable table)
        {
            //II.22.22

            RowIndex = index;
            this.table = table;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_ImplMapRow, table.RowSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(MappingFlags), table.MappingFlagsOffset, MappingFlags, sizeof(short));
                    break;

                case 1:
                    structWriter.WriteMemberForwardedIndex(nameof(MemberForwarded), table.MemberForwardedOffset, MemberForwarded);
                    break;

                case 2:
                    structWriter.WriteStringHeapIndex(nameof(ImportName), table.ImportNameOffset, ImportName);
                    break;

                case 3:
                    structWriter.WriteSimpleIndex(nameof(ImportScope), table.ImportScopeOffset, (int) ImportScope, TableKind.ModuleRef);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"{ImportScopeRow}!{ImportName.GetString()}";
        }
    }
}
