using PESpy.View;

namespace PESpy
{
    public readonly struct IptoStateMapEntry : IValue, IViewable
    {
#if PEFAST
        public int Ip => chunk.PeekInt32(0);
#else
        public int Ip { get; }
#endif

#if PEFAST
        public int State => chunk.PeekInt32(4);
#else
        public int State { get; }
#endif

#if PEFAST
        public int Offset => chunk.PeekInt32(8);
#else
        public int Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //Ip
            sizeof(int);  //State

#if PEFAST
        private readonly MemoryChunk chunk;

        internal IptoStateMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

#else
        internal IptoStateMapEntry(IFileReader reader)
        {
            Offset = (int) reader.Position;

            Ip = reader.ReadInt32();
            State = reader.ReadInt32();
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.IptoStateMapEntry), this, ViewKind.IptoStateMapEntry);

            s.WriteField(nameof(Ip), Ip);
            s.WriteField(nameof(State), State);
        }
    }
}
