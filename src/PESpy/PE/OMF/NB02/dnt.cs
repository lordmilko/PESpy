using System;
using ClrDebug.OMF;

namespace PESpy
{
    //"DirEntry" in cvexefmt.h, "DNT" (Directory eNTry?) in the Microsoft C 6.0 Developer's Toolkit Reference
    public readonly struct dnt
    {
        public SST SubSection => (SST) chunk.PeekUInt16(0);

        public ushort iMod => chunk.PeekUInt16(2);

        public int lfo => chunk.PeekInt32(4);

        public ushort cb => chunk.PeekUInt16(8);

        public object Data { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //SubSection
            sizeof(ushort) + //iMod
            sizeof(int) + //lfo
            sizeof(ushort); //cb

        private readonly MemoryChunk chunk;

        internal dnt(in MemoryChunk chunk, in MemoryChunk outerChunk)
        {
            this.chunk = chunk;
            Data = default;
            Data = GetData(SubSection, outerChunk.Slice(lfo), cb);
        }

        private static object GetData(SST subSection, in MemoryChunk valueChunk, int length)
        {
            switch (subSection)
            {
                case SST.SSTMODULE:
                    return new smd(valueChunk);

                case SST.SSTPUBLIC:
                    return new pbi(valueChunk);
                case SST.SSTSYMBOLS:
                    return OMFReader.ReadNB02Symbols(valueChunk, length);
        public override string ToString()
        {
            return SubSection.ToString();
        }
    }
}
