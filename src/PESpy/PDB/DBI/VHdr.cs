using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("{ulHdr} / {ulVer}")]
    public readonly struct VHdr : IValue, IViewable
    {
        //Enum name made up
        public enum Hdr : uint
        {
            verHdr = 0xeffeeffe
        }

        //Enum name is made up
        public enum Ver : uint
        {
            verLongHash = 1,
            verLongHashV2 = 2,
        }

        public Hdr ulHdr => (Hdr) chunk.PeekUInt32(0);

        public Ver ulVer => (Ver) chunk.PeekUInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ulHdr
            sizeof(int);  //ulVer

        private readonly MemoryChunk chunk;

        internal VHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(VHdr), this, ViewKind.VHdr);

            s.WriteField(nameof(ulHdr), ulHdr, sizeof(int));
            s.WriteField(nameof(ulVer), ulVer, sizeof(int));
        }
    }
}
