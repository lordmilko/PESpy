using PESpy.View;

namespace PESpy.PDB
{
    //SI_PERSIST
    public readonly struct SI_PERSIST : IValue, IViewable
    {
        //cb
        public int ByteCount => chunk.PeekInt32(0);

        //"mpspnpn" = Map of Stream Page Numbers -> Page Numbers. A SPN is simply an index into a PN[], so this is essentially a really
        //complicated way of saying "it's just an array of PN[]". In theory this should be a PN[], but in practice it's a null
        //pointer (0). The actual PN[] for the location of the Stream Table is stored in BIGMSF_HDR.mpspnpnSt. See the PDB README.md in this directory
        //for a full rundown of the way this works.
        public int PageList => chunk.PeekInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) +
            sizeof(int);

        private readonly MemoryChunk chunk;

        internal SI_PERSIST(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(SI_PERSIST), this, ViewKind.SI_PERSIST);

            s.WriteField("cb", ByteCount);
            s.WriteField("mpspnpn", PageList);
        }
    }
}
