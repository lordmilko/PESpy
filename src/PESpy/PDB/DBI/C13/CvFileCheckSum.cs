using System;
using ClrDebug.DIA;

namespace PESpy.PDB
{
    //CV_FileCheckSum (from Roslyn)
    public class CvFileCheckSum //The value of the DEBUG_S_SECTION could be one of several values, so we'll always be boxed
    {
        public int name => chunk.PeekInt32(0);

        public byte len => chunk.PeekByte(4);

        public CV_SourceChksum_t type => (CV_SourceChksum_t) chunk.PeekByte(5);

        public Span<byte> hash => chunk.PeekSpan<byte>(6, len);

        private readonly MemoryChunk chunk;

        internal CvFileCheckSum(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
