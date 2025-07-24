using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct MessageResourceEntry : IValue, IViewable
    {
        public short Length => chunk.PeekInt16(0);

        public MessageResourceFlags Flags => (MessageResourceFlags) chunk.PeekInt16(2);

        public NullTerminatedString Text => chunk.PeekNullTerminatedString(4, Flags switch
        {
            MessageResourceFlags.Unicode => StringKind.UTF16,
            MessageResourceFlags.UTF8 => StringKind.UTF8,
            _ => StringKind.ANSI
        });

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal MessageResourceEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MESSAGE_RESOURCE_ENTRY, this, ViewKind.MessageResourceEntry, Length);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Length), Length);
            s.WriteField(nameof(Flags), Flags, sizeof(short));
            s.WriteNullTerminatedField(nameof(Text), Text);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Text.ToString();
        }
    }
}
