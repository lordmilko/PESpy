using System;
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

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Generation), table.GenerationOffset, Generation);
                    break;

                case 1:
                    structWriter.WriteStringHeapIndex(nameof(Name), table.NameOffset, Name);
                    break;

                case 2:
                    structWriter.WriteGuidHeapIndex(nameof(Mvid), table.MvidOffset, Mvid);
                    break;

                case 3:
                    structWriter.WriteGuidHeapIndex(nameof(EncId), table.EncIdOffset, EncId);
                    break;

                case 4:
                    structWriter.WriteGuidHeapIndex(nameof(EncBaseId), table.EncBaseIdOffset, EncBaseId);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
