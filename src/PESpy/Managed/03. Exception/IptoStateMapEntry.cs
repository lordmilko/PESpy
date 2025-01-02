using PESpy.View;

namespace PESpy
{
    public readonly struct IptoStateMapEntry : IValue, IViewable
    {
        public int Ip { get; }

        public int State { get; }

        public int Offset { get; }

        internal IptoStateMapEntry(IFileReader reader)
        {
            Offset = (int) reader.Position;

            Ip = reader.ReadInt32();
            State = reader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.IptoStateMapEntry), this, ViewKind.IptoStateMapEntry);

            s.WriteField(nameof(Ip), Ip);
            s.WriteField(nameof(State), State);
        }
    }
}
