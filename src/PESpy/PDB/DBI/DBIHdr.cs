using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    public class DBIHdr : IDBIHdr, IValue, IViewable
    {
        private const int snGSSymsOffset = 0;
        private const int snPSSymsOffset = 2;
        private const int snSymRecsOffset = 4;
        private const int cbGpModiOffset = 8;
        private const int cbSCOffset = 12;
        private const int cbSecMapOffset = 16;
        private const int cbFileInfoOffset = 20;

        public SN snGSSyms => (SN) chunk.PeekUInt16(snGSSymsOffset);

        public SN snPSSyms => (SN) chunk.PeekUInt16(snPSSymsOffset);

        public SN snSymRecs => (SN) chunk.PeekUInt16(snSymRecsOffset);

        //6-7: 2 bytes padding

        /// <summary>
        /// size of rgmodi substream
        /// </summary>
        public int cbGpModi => chunk.PeekInt32(cbGpModiOffset);

        /// <summary>
        /// size of Section Contribution substream
        /// </summary>
        public int cbSC => chunk.PeekInt32(cbSCOffset);

        public int cbSecMap => chunk.PeekInt32(cbSecMapOffset);

        public int cbFileInfo => chunk.PeekInt32(cbFileInfoOffset);

        public int Offset => chunk.AbsoluteOffset;

        int IDBIHdr.StructSize => StructSize;

        internal const int StructSize =
            sizeof(short) + //snGSSyms
            sizeof(short) + //snPSSyms
            sizeof(short) + //snSymRecs
            sizeof(short) + //Padding
            sizeof(int) + //cbGpModi
            sizeof(int) + //cbSC
            sizeof(int) + //cbSecMap
            sizeof(int); //cbFileInfo

        private readonly MemoryChunk chunk;

        internal DBIHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.DbiHdr, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(snGSSyms), snGSSymsOffset, snGSSyms);
                    break;

                case 1:
                    structWriter.WriteField(nameof(snPSSyms), snPSSymsOffset, snPSSyms);
                    break;

                case 2:
                    structWriter.WriteField(nameof(snSymRecs), snSymRecsOffset, snSymRecs);
                    break;

                case 3:
                    structWriter.WriteByteBlob(snSymRecsOffset + 2, sizeof(short)); //2 bytes
                    break;

                case 4:
                    structWriter.WriteField(nameof(cbGpModi), cbGpModiOffset, cbGpModi);
                    break;

                case 5:
                    structWriter.WriteField(nameof(cbSC), cbSCOffset, cbSC);
                    break;

                case 6:
                    structWriter.WriteField(nameof(cbSecMap), cbSecMapOffset, cbSecMap);
                    break;

                case 7:
                    structWriter.WriteField(nameof(cbFileInfo), cbFileInfoOffset, cbFileInfo);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
