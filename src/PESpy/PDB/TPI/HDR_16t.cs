using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //Used when the TPI version is <= impv41
    public class HDR_16t : IHDR //Header could either be HDR or HDR_16t
    {
        private const int versOffset = 0;
        private const int tiMinOffset = 4;
        private const int tiMacOffset = 6;
        private const int cbGprecOffset = 8;
        private const int snHashOffset = 12;

        public TPIImpv vers
        {
            get => (TPIImpv) chunk.PeekUInt32(versOffset);
            set => chunk.PokeUInt32(versOffset, (uint) value);
        }

        public ushort tiMin
        {
            get => chunk.PeekUInt16(tiMinOffset);
            set => chunk.PokeUInt16(tiMinOffset, value);
        }

        public ushort tiMac
        {
            get => chunk.PeekUInt16(tiMacOffset);
            set => chunk.PokeUInt16(tiMacOffset, value);
        }

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

        int IHDR.StructSize => StructSize;

        // rest of file is "REC gprec[];"

        internal const int StructSize =
            sizeof(int) + //vers
            sizeof(short) + //tiMin
            sizeof(short) + //tiMac
            sizeof(int) + //cbGprec
            sizeof(short) + //snHash
            sizeof(short); //Padding

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal HDR_16t(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.HDR_16t, this, ViewKind.Hdr_16t, StructSize);

        int IViewable.NumChildren => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(vers), versOffset, vers, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(tiMin), tiMinOffset, tiMin);
                    break;

                case 2:
                    structWriter.WriteField(nameof(tiMac), tiMacOffset, tiMac);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbGprec), cbGprecOffset, cbGprec);
                    break;

                case 4:
                    structWriter.WriteField(nameof(snHash), snHashOffset, snHash);
                    break;

                case 5:
                    structWriter.WriteByteBlob(snHashOffset + 2, sizeof(short));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
