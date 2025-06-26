using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //HDR

    /// <summary>
    /// type database header
    /// </summary>
    public class HDR : IHDR //Header could either be HDR or HDR_16
    {
        /// <summary>
        /// version which created this TypeServer
        /// </summary>
        public TPIImpv vers
        {
            get => (TPIImpv) chunk.PeekUInt32(0);
            set => chunk.PokeUInt32(0, (uint) value);
        }

        /// <summary>
        /// size of the header, allows easier upgrading and backwards compatibility
        /// </summary>
        public int cbHdr
        {
            get => chunk.PeekInt32(4);
            set => chunk.PokeInt32(4, value);
        }

        /// <summary>
        /// lowest TI
        /// </summary>
        public CV_typ_t tiMin
        {
            get => chunk.PeekInt32(8);
            set => chunk.PokeInt32(8, value);
        }

        /// <summary>
        /// highest TI + 1
        /// </summary>
        public CV_typ_t tiMac
        {
            get => chunk.PeekInt32(12);
            set => chunk.PokeInt32(12, value);
        }

        /// <summary>
        /// count of bytes used by the gprec which follows.
        /// </summary>
        public int cbGprec
        {
            get => chunk.PeekInt32(16);
            set => chunk.PokeInt32(16, value);
        }

        /// <summary>
        /// hash stream schema
        /// </summary>
        public TpiHash tpihash => new TpiHash(chunk.Slice(20));

        int IHDR.StructSize => StructSize;

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //vers
            sizeof(int) + //cbHdr
            sizeof(int) + //tiMin
            sizeof(int) + //tiMac
            sizeof(int) + //cbGprec
            TpiHash.StructSize; //tpihash

        private readonly MemoryChunk chunk;

        internal HDR(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(HDR), this, ViewKind.Hdr, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(vers), vers, sizeof(int));
            s.WriteField(nameof(cbHdr), cbHdr);
            s.WriteField(nameof(tiMin), tiMin);
            s.WriteField(nameof(tiMac), tiMac);
            s.WriteStructField(nameof(tpihash), tpihash);

            return s.ToArray();
        }
    }
}
