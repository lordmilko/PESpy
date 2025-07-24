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

        internal int StructSize =>
            sizeof(short) + //cSeg
            sizeof(short) + //cSegLog
            (OMFSegDesc.StructSize * cSeg);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(cSeg), cSeg);
            s.WriteField(nameof(cSegLog), cSegLog);
            s.WriteInline(rgDesc);

            return s.ToArray();
        }
    }
}
