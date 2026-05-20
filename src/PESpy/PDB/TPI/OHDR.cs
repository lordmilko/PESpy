using System;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct OHDR : IViewableValue
    {
        private const int szMagicOffset = 0;
        private const int versOffset = 44;
        private const int sigOffset = 48;
        private const int ageOffset = 52;
        private const int tiMinOffset = 56;
        private const int tiMacOffset = 58;
        private const int cbOffset = 60;

        //I'm not sure where they got this from this definitely looks like Microsoft style code
        //https://github.com/Paolo-Maffei/OpenNT/blob/master/sdktools/vctools/pdb/dbi/tpi.cpp#L611

        internal const string OHdrMagic = "Microsoft C/C++ program database 1.00\r\n\u001aJG\0\0";

        //szMagic
        public FixedAnsiString szMagic => chunk.PeekAnsiFixedLength(szMagicOffset, 44);

        /// <summary>
        /// version which created this file
        /// </summary>
        public INTV vers => (INTV) chunk.PeekInt32(versOffset);

        /// <summary>
        /// signature
        /// </summary>
        public int sig => chunk.PeekInt32(sigOffset);

        /// <summary>
        /// age (no. of times written)
        /// </summary>
        public int age => chunk.PeekInt32(ageOffset);

        /// <summary>
        /// lowest TI
        /// </summary>
        public ushort tiMin => chunk.PeekUInt16(tiMinOffset);

        /// <summary>
        /// highest TI + 1
        /// </summary>
        public ushort tiMac => chunk.PeekUInt16(tiMacOffset);

        /// <summary>
        /// count of bytes used by the gprec which follows.
        /// </summary>
        public int cb => chunk.PeekInt32(cbOffset);

        //rest of file is "REC gprec[];"

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            44 +          //Magic
            sizeof(int) + //vers
            sizeof(int) + //sig
            sizeof(int) + //age
            sizeof(ushort) + //tiMin
            sizeof(ushort) + //tiMac
            sizeof(int); //cb

        private readonly MemoryChunk chunk;

        internal OHDR(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OHDR, StructSize);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteAnsiFixedLengthField(nameof(szMagic), szMagicOffset, szMagic);
                    break;

                case 1:
                    structWriter.WriteField(nameof(vers), versOffset, vers, sizeof(int));
                    break;

                case 2:
                    structWriter.WriteField(nameof(sig), sigOffset, sig);
                    break;

                case 3:
                    structWriter.WriteField(nameof(age), ageOffset, age);
                    break;

                case 4:
                    structWriter.WriteField(nameof(tiMin), tiMinOffset, tiMin);
                    break;

                case 5:
                    structWriter.WriteField(nameof(tiMac), tiMacOffset, tiMac);
                    break;

                case 6:
                    structWriter.WriteField(nameof(cb), cbOffset, cb);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
