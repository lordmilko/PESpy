using PESpy.View;

namespace PESpy.PDB
{
    public class OMFSegMap : IValue, IViewable //Class as it may not be present
    {
        public short cSeg => chunk.PeekInt16(0);

        public short cSegLog => chunk.PeekInt16(2);

        private OMFSegMapDesc[]? descs;

        public OMFSegMapDesc[] rgDesc
        {
            get
            {
                if (descs == null)
                {
                    var numSegs = cSeg;

                    var arr = new OMFSegMapDesc[numSegs];

                    for (var i = 0; i < numSegs; i++)
                        arr[i] = new OMFSegMapDesc(chunk.Slice(4 + (i * OMFSegMapDesc.StructSize)));

                    descs = arr;
                }

                return descs;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal OMFSegMap(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            descs = null;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(OMFSegMap), this, ViewKind.OMFSegMap);

            s.WriteField(nameof(cSeg), cSeg);
            s.WriteField(nameof(cSegLog), cSegLog);
            s.WriteInline(rgDesc);
        }
    }
}
