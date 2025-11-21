using System.Diagnostics;

namespace PESpy.SYM
{
    //endmap_s
    //SYM is too obscure, so I think it's better to use the real name
    [DebuggerDisplay("em_spmap = {em_spmap}, em_ver = {em_ver}, em_rel = {em_rel}")]
    [Source(SourceKind.mapsym_h)]
    public readonly struct endmap_s
    {
        /// <summary>
        /// end of map chain (SEG ptr 0)
        /// </summary>
        public ushort em_spmap => chunk.PeekUInt16(0);

        /// <summary>
        /// release
        /// </summary>
        public byte em_rel => chunk.PeekByte(2);

        /// <summary>
        /// version
        /// </summary>
        public byte em_ver => chunk.PeekByte(3);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //em_spmap
            sizeof(byte) + //em_rel
            sizeof(byte); //version

        private readonly MemoryChunk chunk;

        internal endmap_s(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
