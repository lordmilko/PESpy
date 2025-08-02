using PESpy.View;

namespace PESpy
{
    public readonly struct IptoStateMapEntry : IValue, IViewable
    {
        public int Ip => chunk.PeekInt32(0);

        public int State => chunk.PeekInt32(4);

        public int Offset => chunk.PeekInt32(8);

        internal const int StructSize =
            sizeof(int) + //Ip
            sizeof(int);  //State

        private readonly MemoryChunk chunk;

        internal IptoStateMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

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
