using PESpy.View;

namespace PESpy.PDB
{
    public class SC : SC40
    {
        public int dwDataCrc => chunk.PeekInt32(20);

        public int dwRelocCrc => chunk.PeekInt32(24);

        internal new const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort) + //padding2
            sizeof(int) + //dwDataCrc
            sizeof(int); //dwRelocCrc

        internal SC(in MemoryChunk chunk) : base(chunk)
        {
        }

        protected override void WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(SC), this, ViewKind.SC);

            s.WriteField(nameof(isect), isect);
            s.WriteField(nameof(padding1), padding1);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(dwCharacteristics), dwCharacteristics, sizeof(int));
            s.WriteField(nameof(imod), imod);
            s.WriteField(nameof(padding2), padding2);
            s.WriteField(nameof(dwDataCrc), dwDataCrc);
            s.WriteField(nameof(dwRelocCrc), dwRelocCrc);
        }
    }
}
