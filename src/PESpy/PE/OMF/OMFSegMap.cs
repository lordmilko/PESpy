using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.cvexefmt)]
    public class OMFSegMap : IValue, IViewable //Class as it may not be present
    {
        private const int cSegOffset = 0;
        private const int cSegLogOffset = 2;
        private const int rgDescOffset = 4;

        public short cSeg => chunk.PeekInt16(cSegOffset);

        public short cSegLog => chunk.PeekInt16(cSegLogOffset);

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
                        arr[i] = new OMFSegMapDesc(chunk.Slice(rgDescOffset + (i * OMFSegMapDesc.StructSize)));

                    descs = arr;
                }

                return descs;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(short) + //cSeg
            sizeof(short) + //cSegLog
            (OMFSegMapDesc.StructSize * cSeg);

        private readonly MemoryChunk chunk;

        internal OMFSegMap(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            descs = null;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.OMFSegMap, this, ViewKind.OMFSegMap, StructSize);

        int IViewable.NumChildren() => 2 + rgDesc.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(cSeg), cSegOffset, cSeg);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cSegLog), cSegLogOffset, cSegLog);
                    break;

                default:
                    structWriter.WriteInline(rgDesc[index - 2]);
                    break;
            }
        }
    }
}
