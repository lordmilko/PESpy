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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IptoStateMapEntry, this, ViewKind.IptoStateMapEntry, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Ip), Ip);
            s.WriteField(nameof(State), State);

            return s.ToArray();
        }
    }
}
