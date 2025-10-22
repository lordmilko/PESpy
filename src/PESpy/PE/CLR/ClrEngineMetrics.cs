using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public class ClrEngineMetrics : IValue, IViewable
    {
        private const int SizeOffset = 0;
        private const int DbiVersionOffset = 4;
        private const int ContinueStartupEventOffset = 8;

        public int Size => chunk.PeekInt32(SizeOffset);
        public int DbiVersion => chunk.PeekInt32(DbiVersionOffset);
        public ulong ContinueStartupEvent => chunk.PeekPointer(ContinueStartupEventOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //Size
            sizeof(int) + //DbiVersion
            (is32Bit ? 4 : 8); //ContinueStartupEvent

        private readonly MemoryChunk chunk;

        internal ClrEngineMetrics(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.CLR_ENGINE_METRICS, this, ViewKind.ClrEngineMetrics, StructSize(((PEViewWriter) writer).Is32Bit));

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    break;

                case 1:
                    structWriter.WriteField(nameof(DbiVersion), DbiVersionOffset, DbiVersion);
                    break;

                case 2:
                    structWriter.WritePointerField(nameof(ContinueStartupEvent), ContinueStartupEventOffset, ContinueStartupEvent);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
