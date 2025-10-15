using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public class MessageResourceData : IValue, IViewable
    {
        private const int NumberOfBlocksOffset = 0;

        public int NumberOfBlocks => chunk.PeekInt32(NumberOfBlocksOffset);

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

        int IViewable.NumChildren => 1 + Blocks.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(NumberOfBlocks), NumberOfBlocksOffset, NumberOfBlocks);
                    break;

                default:
                    structWriter.WriteInline(Blocks[index - 1]);
                    break;
            }
        }
    }
}
