using System;
using PESpy.View;

namespace PESpy
{
    //MESSAGE_RESOURCE_ENTRY
    [Source(SourceKind.winnt_h)]
    public readonly struct MessageResourceEntry : IValue, IViewable
    {
        private const int LengthOffset = 0;
        private const int FlagsOffset = 2;
        private const int TextOffset = 4;

        public short Length => chunk.PeekInt16(LengthOffset);

        public MessageResourceFlags Flags => (MessageResourceFlags) chunk.PeekInt16(FlagsOffset);

        public NullTerminatedString Text => chunk.PeekNullTerminatedString(TextOffset, Flags switch
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
            writer.NewStruct(this, ViewKind.MessageResourceEntry, Length);

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(3, Length);

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Length), LengthOffset, Length);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Flags), FlagsOffset, Flags, sizeof(short));
                    break;

                case 2:
                    structWriter.WriteNullTerminatedField(nameof(Text), TextOffset, Text);
                    break;

                case 3:
                    //Possible alignment
                    structWriter.AlignOrThrow(Length);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Text.ToString();
        }
    }
}
