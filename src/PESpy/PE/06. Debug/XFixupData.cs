using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("Type = {Type}, Rva = {Rva}, RvaTarget = {RvaTarget}, Extra = {Extra}")]
    public readonly struct XFixupData : IValue
    {
        //PEAnatomist thinks that these types correspond with the IMAGE_REL_* type used in ImageRelocation. So if the IMAGE_FILE_MACHINE
        //is I386, use ImageRelI386
        public short Type => chunk.PeekInt16(0);

        public short Extra => chunk.PeekInt16(2);

        public int Rva => chunk.PeekInt32(4);

        public int RvaTarget => chunk.PeekInt32(8);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) +
            sizeof(short) +
            sizeof(int) +
            sizeof(int);

        private readonly MemoryChunk chunk;

        internal XFixupData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
