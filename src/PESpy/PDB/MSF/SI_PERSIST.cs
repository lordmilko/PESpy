using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //SI_PERSIST
    [Source(SourceKind.msf_cpp)]
    public readonly struct SI_PERSIST : IViewableValue
    {
        private const int ByteCountOffset = 0;
        private const int PageListOffset = 4;

        //cb
        public int ByteCount //st.mpsnsi[snSt].cb
        {
            get => chunk.PeekInt32(ByteCountOffset);
            set => chunk.PokeInt32(ByteCountOffset, value);
        }

        //"mpspnpn" = Map of Stream Page Numbers -> Page Numbers. A SPN is simply an index into a PN[], so this is essentially a really
        //complicated way of saying "it's just an array of PN[]". In theory this should be a PN[], but in practice it's a null
        //pointer (0). The actual PN[] for the location of the Stream Table is stored in BIGMSF_HDR.mpspnpnSt. See the PDB README.md in this directory
        //for a full rundown of the way this works. MSF_HB::Commit explicitly sets this to 0. mpspnpnSt comes from siPnList
        public int PageList
        {
            get => chunk.PeekInt32(PageListOffset);
            set => chunk.PokeInt32(PageListOffset, value);
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) +
            sizeof(int);

        private readonly MemoryChunk chunk;

        internal SI_PERSIST(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.SI_PERSIST, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("cb", ByteCountOffset, ByteCount);
                    break;

                case 1:
                    structWriter.WriteField("mpspnpn", PageListOffset, PageList);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
