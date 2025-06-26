using PESpy.View;

namespace PESpy.PDB
{
    public class DBIHdr : IDBIHdr, IValue, IViewable
    {
        public SN snGSSyms => (SN) chunk.PeekUInt16(0);

        public SN snPSSyms => (SN) chunk.PeekUInt16(2);

        public SN snSymRecs => (SN) chunk.PeekUInt16(4);

        //6-7: 2 bytes padding

        /// <summary>
        /// size of rgmodi substream
        /// </summary>
        public int cbGpModi => chunk.PeekInt32(8);

        /// <summary>
        /// size of Section Contribution substream
        /// </summary>
        public int cbSC => chunk.PeekInt32(12);

        public int cbSecMap => chunk.PeekInt32(16);

        public int cbFileInfo => chunk.PeekInt32(20);

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
            writer.NewStruct(nameof(DBIHdr), this, ViewKind.DbiHdr, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(snGSSyms), snGSSyms);
            s.WriteField(nameof(snPSSyms), snPSSyms);
            s.WriteField(nameof(snSymRecs), snSymRecs);
            s.Align(4);
            s.WriteField(nameof(cbGpModi), cbGpModi);
            s.WriteField(nameof(cbSC), cbSC);
            s.WriteField(nameof(cbSecMap), cbSecMap);
            s.WriteField(nameof(cbFileInfo), cbFileInfo);

            return s.ToArray();
        }
    }
}
