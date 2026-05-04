using System;
using ClrDebug;
using PESpy.View;

namespace PESpy.NativeAOT
{
    //I'm not sure what release this is part of. Maybe 10.2? But it may have also been backported to
    //earlier versions and may be present in any minor release from February 2026
    public struct ModuleInfoRowV2 : IModuleInfoRow
    {
        private const int SectionIdOffset = 0;
        private const int LengthOffset = 4;
        private const int StartOffset = 8;

        public ReadyToRunSectionType SectionId => (ReadyToRunSectionType) chunk.PeekUInt32(SectionIdOffset);

        public int Length => chunk.PeekInt32(LengthOffset);

        public long Start => (long) chunk.PeekPointer(StartOffset);

        long IModuleInfoRow.End => Start + Length;

        private VA<IValue> data;

        public VA<IValue> Data => ModuleInfoRowV1.GetData(SectionId, ref data, chunk, Start, Length);

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //SectionId
            sizeof(int) + //Length
            (is32Bit ? 4 : 8); //Start

        private readonly MemoryChunk chunk;

        internal ModuleInfoRowV2(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer) => ModuleInfoRowV1.WriteGlobals(writer, this);

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ModuleInfoRowV1, StructSize(writer.Is32Bit));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(SectionId), SectionIdOffset, SectionId, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(Length), LengthOffset, Length);
                    break;

                case 2:
                    structWriter.WritePointerField(nameof(Start), StartOffset, Start);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return SectionId.ToString();
        }
    }
}
