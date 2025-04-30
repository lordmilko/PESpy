using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct ECInfo : IValue, IViewable
    {
        //These name indices point into the Edit and Continue Name Table info included in the DBI

        public int niSrcFile => chunk.PeekInt32(0);

        public int niPdbFile => chunk.PeekInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //niSrcFile
            sizeof(int);  //niPdbFile

        private readonly MemoryChunk chunk;

        internal ECInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(ECInfo), this, ViewKind.ECInfo);

            s.WriteField(nameof(niSrcFile), niSrcFile);
            s.WriteField(nameof(niPdbFile), niPdbFile);
        }
    }
}
