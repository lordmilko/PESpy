using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //Arguably this could be a struct, but we really need to have a non-allocating
    //FrameData list type first, so for now leave as class. The rules around using classes
    //due to potential boxing no longer apply, since we now have CvDebugSSubsectionHeader.GetData<T>()
    public class RvaAndFrameData : IViewableValue
    {
        private const int RVAOffset = 0;
        public int RVA => chunk.PeekInt32(RVAOffset);

        private FrameData[]? frameData;

        //We could use a custom list type here to reduce allocations
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
            writer.NewStruct(this, ViewKind.RvaAndFrameData, StructSize);

        int IViewable.NumChildren() => 1 + FrameData.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RVA), RVAOffset, RVA);
                    break;

                default:
                    structWriter.WriteInline(FrameData[index - 1]);
                    break;
            }
        }
    }
}
