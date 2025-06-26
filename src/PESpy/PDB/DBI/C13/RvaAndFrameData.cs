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

        internal int StructSize =>
            sizeof(int) +
            length;

        private readonly MemoryChunk chunk;
        private int length;

        internal RvaAndFrameData(in MemoryChunk chunk, int length)
        {
            this.chunk = chunk;
            this.length = length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("RVA + FrameData", this, ViewKind.RvaAndFrameData, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(RVA), RVA);
            s.WriteInline(FrameData);

            return s.ToArray();
        }
    }
}
