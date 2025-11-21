using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Used when the TPI version is == impv50Interim
    public class HDR_VC50Interim : IHDR
    {
        private const int versOffset = 0;
        private const int tiMinOffset = 4;
        private const int tiMacOffset = 8;
        private const int cbGprecOffset = 12;
        private const int snHashOffset = 16;

        /// <summary>
        /// version which created this TypeServer
        /// </summary>
        public TPIImpv vers
        {
            get => (TPIImpv) chunk.PeekUInt32(versOffset);
            set => chunk.PokeUInt32(versOffset, (uint) value);
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

        public SN snHash
        {
            get => chunk.PeekUInt16(snHashOffset);
            set => chunk.PokeUInt16(snHashOffset, value);
        }

        CV_typ_t IHDR.tiMin => tiMin;
        CV_typ_t IHDR.tiMac => tiMac;
        int IHDR.StructSize => StructSize;

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //vers
            sizeof(int) + //tiMin
            sizeof(int) + //tiMac
            sizeof(int) + //cbGprec
            sizeof(short) + //snHash
            sizeof(short); //Padding

        private readonly MemoryChunk chunk;

        internal HDR_VC50Interim(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            throw new System.NotImplementedException();

        int IViewable.NumChildren() =>
            throw new NotImplementedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            throw new NotImplementedException();
        }
    }
}
