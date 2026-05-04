using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    [DebuggerDisplay("{ulHdr} / {ulVer}")]
    public readonly struct VHdr : IViewableValue
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

        private const int ulHdrOffset = 0;
        private const int ulVerOffset = 4;

        public Hdr ulHdr => (Hdr) chunk.PeekUInt32(ulHdrOffset);

        public Ver ulVer => (Ver) chunk.PeekUInt32(ulVerOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ulHdr
            sizeof(int);  //ulVer

        private readonly MemoryChunk chunk;

        internal VHdr(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VHdr, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ulHdr), ulHdrOffset, ulHdr, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(ulVer), ulVerOffset, ulVer, sizeof(int));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
