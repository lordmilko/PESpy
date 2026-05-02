using System;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt_h)]
    public readonly struct OMFSourceLine : IViewableValue
    {
        private const int SegOffset = 0;
        private const int cLnOffOffset = 2;
        private const int offsetOffset = 4;
        private int lineNbrOffset => offsetOffset + (cLnOff * sizeof(int));

        public ushort Seg => chunk.PeekUInt16(SegOffset);

        public ushort cLnOff => chunk.PeekUInt16(cLnOffOffset);

        //Arguably this should have an xref to the RVA pointed to by the given off/seg (seg listed above)
        public NativeSpan<int> offset => chunk.PeekNativeSpan<int>(offsetOffset, cLnOff);

        public NativeSpan<ushort> lineNbr => chunk.PeekNativeSpan<ushort>(lineNbrOffset, cLnOff);

        public int Offset => chunk.AbsoluteOffset;

        private int StructSize =>
            sizeof(short) + //Seg
            sizeof(short) + //cLnOff
            cLnOff * (sizeof(int) + sizeof(short));

        private readonly MemoryChunk chunk;

        internal OMFSourceLine(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OMFSourceLine, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Seg), SegOffset, Seg);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cLnOff), cLnOffOffset, cLnOff);
                    break;

                case 2:
                    structWriter.WriteField(nameof(offset), offsetOffset, offset);
                    break;

                case 3:
                    structWriter.WriteField(nameof(lineNbr), lineNbrOffset, lineNbr);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
