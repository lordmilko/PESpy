using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct OffCb : IValue, IViewable
    {
        public int off => chunk.PeekInt32(0);

        public int cb => chunk.PeekInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal OffCb(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(OffCb), this, ViewKind.OffCb);

            s.WriteField(nameof(off), off);
            s.WriteField(nameof(cb), cb);
        }
    }
}
