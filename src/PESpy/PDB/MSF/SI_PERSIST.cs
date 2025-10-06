using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //SI_PERSIST
    public readonly struct SI_PERSIST : IValue, IViewable
    {
        //cb
        public int ByteCount //st.mpsnsi[snSt].cb
        {
            get => chunk.PeekInt32(0);
            set => chunk.PokeInt32(0, value);
        }

        //"mpspnpn" = Map of Stream Page Numbers -> Page Numbers. A SPN is simply an index into a PN[], so this is essentially a really
        //complicated way of saying "it's just an array of PN[]". In theory this should be a PN[], but in practice it's a null
        //pointer (0). The actual PN[] for the location of the Stream Table is stored in BIGMSF_HDR.mpspnpnSt. See the PDB README.md in this directory
        //for a full rundown of the way this works. MSF_HB::Commit explicitly sets this to 0. mpspnpnSt comes from siPnList
        public int PageList
        {
            get => chunk.PeekInt32(4);
            set => chunk.PokeInt32(4, value);
        }

        public int Offset => chunk.AbsoluteOffset;

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
            writer.NewStruct(Strings.SI_PERSIST, this, ViewKind.SI_PERSIST, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("cb", ByteCount);
            s.WriteField("mpspnpn", PageList);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
