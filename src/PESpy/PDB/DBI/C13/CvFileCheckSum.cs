using System;
using ClrDebug.DIA;
using PESpy.View;

namespace PESpy.PDB
{
    //CV_FileCheckSum (from Roslyn)
    public class CvFileCheckSum : IValue, IViewable //The value of the DEBUG_S_SECTION could be one of several values, so we'll always be boxed
    {
        public int name => chunk.PeekInt32(0);

        public byte len => chunk.PeekByte(4);

        public CV_SourceChksum_t type => (CV_SourceChksum_t) chunk.PeekByte(5);

        public NativeSpan<byte> hash => chunk.PeekNativeSpan<byte>(6, len);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal CvFileCheckSum(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("CV_FileCheckSum", this, ViewKind.CvFileCheckSum);

            s.WriteField(nameof(name), name);
            s.WriteField(nameof(len), len);
            s.WriteField(nameof(type), type, sizeof(byte));
            s.WriteField(nameof(hash), hash);
        }
    }
}
