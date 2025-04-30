using PESpy.View;

namespace PESpy.PDB
{
    public class RvaAndFrameData : IValue, IViewable //Will be boxed so should be class
    {
        public int RVA => chunk.PeekInt32(0);

        private FrameData[]? frameData;

        public FrameData[] FrameData
        {
            get
            {
                if (frameData == null)
                {
                    var results = new FrameData[(length - 4) / PESpy.PDB.FrameData.StructSize];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new FrameData(chunk.Slice(4 + (i * PESpy.PDB.FrameData.StructSize)));

                    frameData = results;
                }

                return frameData;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private int length;

        internal RvaAndFrameData(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("RVA + FrameData", this, ViewKind.RvaAndFrameData);

            s.WriteField(nameof(RVA), RVA);
            s.WriteInline(FrameData);
        }
    }
}
