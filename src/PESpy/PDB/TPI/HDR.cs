using System;
using System.Diagnostics;
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
        private const int versOffset = 0;
        private const int cbHdrOffset = 4;
        private const int tiMinOffset = 8;
        private const int tiMacOffset = 12;
        private const int cbGprecOffset = 16;
        private const int tpihashOffset = 20;

        /// <summary>
        /// version which created this TypeServer
        /// </summary>
        public TPIImpv vers
        {
            get => (TPIImpv) chunk.PeekUInt32(versOffset);
            set => chunk.PokeUInt32(versOffset, (uint) value);
        }

        /// <summary>
        /// size of the header, allows easier upgrading and backwards compatibility
        /// </summary>
        public int cbHdr
        {
            get => chunk.PeekInt32(cbHdrOffset);
            set => chunk.PokeInt32(cbHdrOffset, value);
        }

        /// <summary>
        /// lowest TI
        /// </summary>
        public CV_typ_t tiMin
        {
            get => chunk.PeekInt32(tiMinOffset);
            set => chunk.PokeInt32(tiMinOffset, value);
        }

        /// <summary>
        /// highest TI + 1
        /// </summary>
        public CV_typ_t tiMac
        {
            get => chunk.PeekInt32(tiMacOffset);
            set => chunk.PokeInt32(tiMacOffset, value);
        }

        /// <summary>
        /// count of bytes used by the gprec which follows.
        /// </summary>
        public int cbGprec
        {
            get => chunk.PeekInt32(cbGprecOffset);
            set => chunk.PokeInt32(cbGprecOffset, value);
        }

        /// <summary>
        /// hash stream schema
        /// </summary>
        public TpiHash tpihash => new TpiHash(chunk.Slice(tpihashOffset));

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
            writer.NewStruct(Strings.HDR, this, ViewKind.Hdr, StructSize);

        int IViewable.NumChildren => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(vers), versOffset, vers, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(cbHdr), cbHdrOffset, cbHdr);
                    break;

                case 2:
                    structWriter.WriteField(nameof(tiMin), tiMinOffset, tiMin);
                    break;

                case 3:
                    structWriter.WriteField(nameof(tiMac), tiMacOffset, tiMac);
                    break;

                case 4:
                    structWriter.WriteField(nameof(cbGprec), cbGprecOffset, cbGprec);
                    break;

                case 5:
                    structWriter.WriteStructField(nameof(tpihash), tpihashOffset, tpihash);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
