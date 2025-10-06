using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public class MessageResourceData : IValue, IViewable
    {
        public int NumberOfBlocks => chunk.PeekInt32(0);

        private MessageResourceBlock[]? blocks;

        public MessageResourceBlock[] Blocks
        {
            get
            {
                if (blocks == null)
                {
                    var results = new MessageResourceBlock[NumberOfBlocks];

                    var rootRVA = chunk.AbsoluteOffset;

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new MessageResourceBlock(chunk.Slice(4 + (i * MessageResourceBlock.StructSize)), rootRVA);

                    blocks = results;
                }

                return blocks;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal MessageResourceData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.RelayGlobals(Blocks);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MESSAGE_RESOURCE_DATA, this, ViewKind.MessageResourceData, sizeof(int) + (NumberOfBlocks * MessageResourceBlock.StructSize));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(NumberOfBlocks), NumberOfBlocks);
            s.WriteInline(Blocks);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
