using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("Type = {Type}, Rva = {Rva}, RvaTarget = {RvaTarget}, Extra = {Extra}")]
    public readonly struct XFixupData : IValue
    {
#if PEFAST
        public short Type => chunk.PeekInt16(0);
#else
        public short Type { get; }
#endif

#if PEFAST
        public short Extra => chunk.PeekInt16(2);
#else
        public short Extra { get; }
#endif

#if PEFAST
        public int Rva => chunk.PeekInt32(4);
#else
        public int Rva { get; }
#endif

#if PEFAST
        public int RvaTarget => chunk.PeekInt32(8);
#else
        public int RvaTarget { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        internal const int StructSize =
            sizeof(short) +
            sizeof(short) +
            sizeof(int) +
            sizeof(int);

#if PEFAST
        private readonly MemoryChunk chunk;

        internal XFixupData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal XFixupData(IFileReader reader)
        {
            Offset = (int) reader.Position;

            //PEAnatomist thinks that these types correspond with the IMAGE_REL_* type used in ImageRelocation. So if the IMAGE_FILE_MACHINE
            //is I386, use ImageRelI386
            Type = reader.ReadInt16();
            Extra = reader.ReadInt16();
            Rva = reader.ReadInt32();
            RvaTarget = reader.ReadInt32();
        }
#endif
    }
}
