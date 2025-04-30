using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Used when the TPI version is <= impv41
    public class HDR_16t : IHDR //Header could either be HDR or HDR_16t
    {
        public TPIImpv vers => (TPIImpv) chunk.PeekUInt32(0);

        public ushort tiMin => chunk.PeekUInt16(4);

        public ushort tiMac => chunk.PeekUInt16(6);

        public int cbGprec => chunk.PeekInt32(8);

        public SN snHash => chunk.PeekUInt16(12);

        int IHDR.StructSize => StructSize;

        // rest of file is "REC gprec[];"

        internal const int StructSize =
            sizeof(int) + //vers
            sizeof(short) + //tiMin
            sizeof(short) + //tiMac
            sizeof(int) + //cbGprec
            sizeof(short) + //snHash
            sizeof(short); //Padding

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal HDR_16t(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(HDR_16t), this, ViewKind.Hdr_16t);

            s.WriteField(nameof(vers), vers, sizeof(int));
            s.WriteField(nameof(tiMin), tiMin);
            s.WriteField(nameof(tiMac), tiMac);
            s.WriteField(nameof(cbGprec), cbGprec);
            s.WriteField(nameof(snHash), snHash);
            s.Align(4);
        }
    }
}
