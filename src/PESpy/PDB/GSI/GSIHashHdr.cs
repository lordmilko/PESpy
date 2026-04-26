using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    public enum GSIHashSCImpv : uint
    {
        GSIHashSCImpvV70 = 0xeffe0000 + 19990810
    }

    public class GSIHashHdr : IValue, IViewable //May not be present
    {
        public const int hdrSignature = -1;
        private const int verSignatureOffset = 0;
        private const int verHdrOffset = 4;
        private const int cbHrOffset = 8;
        private const int cbBucketsOffset = 12;

        public int verSignature => chunk.PeekInt16(verSignatureOffset);

        public GSIHashSCImpv verHdr => (GSIHashSCImpv) chunk.PeekUInt32(verHdrOffset);

        public int cbHr => chunk.PeekInt32(cbHrOffset);

        public int cbBuckets => chunk.PeekInt32(cbBucketsOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //verSignature
            sizeof(int) + //verHdr
            sizeof(int) + //cbHr
            sizeof(int);  //cbBuckets

        private readonly MemoryChunk chunk;

        internal GSIHashHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.GSIHashHdr, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(verSignature), verSignatureOffset, verSignature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(verHdr), verHdrOffset, verHdr, sizeof(int));
                    break;

                case 2:
                    structWriter.WriteField(nameof(cbHr), cbHrOffset, cbHr);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbBuckets), cbBucketsOffset, cbBuckets);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
