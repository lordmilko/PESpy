using ClrDebug.PDB;

namespace PESpy.PDB
{
    public readonly struct OHDR
    {
        //I'm not sure where they got this from this definitely looks like Microsoft style code
        //https://github.com/Paolo-Maffei/OpenNT/blob/master/sdktools/vctools/pdb/dbi/tpi.cpp#L611

        internal const string OHdrMagic = "Microsoft C/C++ program database 1.00\r\n\u001aJG\0\0";

        //szMagic
        public FixedAnsiString Magic => chunk.PeekAnsiFixedLength(0, 44);

        /// <summary>
        /// version which created this file
        /// </summary>
        public INTV vers => (INTV) chunk.PeekInt32(44);

        /// <summary>
        /// signature
        /// </summary>
        public int sig => chunk.PeekInt32(48);

        /// <summary>
        /// age (no. of times written)
        /// </summary>
        public int age => chunk.PeekInt32(52);

        /// <summary>
        /// lowest TI
        /// </summary>
        public ushort tiMin => chunk.PeekUInt16(56);

        /// <summary>
        /// highest TI + 1
        /// </summary>
        public ushort tiMac => chunk.PeekUInt16(58);

        /// <summary>
        /// count of bytes used by the gprec which follows.
        /// </summary>
        public int cb => chunk.PeekInt32(60);

        //rest of file is "REC gprec[];"

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
    }
}
