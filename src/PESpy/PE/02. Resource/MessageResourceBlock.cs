using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("0x{LowId.ToString(\"X\"),nq} - 0x{HighId.ToString(\"X\"),nq}")]
    public struct MessageResourceBlock : IValue, IViewable
    {
        private const int LowIdOffset = 0;
        private const int HighIdOffset = 4;
        private const int OffsetToEntriesOffset = 8;

        //Values are regularly uints
        public uint LowId => chunk.PeekUInt32(LowIdOffset);

        public uint HighId => chunk.PeekUInt32(HighIdOffset);

        private RVA<MessageResourceEntry[]> offsetToEntries;

        public RVA<MessageResourceEntry[]> OffsetToEntries
        {
            get
            {
                if (offsetToEntries.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(OffsetToEntriesOffset);

                    var absoluteRva = rootRVA + rva;

                    var valueChunk = new MemoryChunk(chunk.block, absoluteRva - chunk.block.RemoteStartOffset);

                    using var list = new PooledList<MessageResourceEntry>();

                    var offset = 0;

                    for (var i = LowId; i <= HighId; i++)
                    {
                        var entry = new MessageResourceEntry(valueChunk.Slice(offset));

                        offset += entry.Length;

                        list.Add(entry);
                    }

                    offsetToEntries = new RVA<MessageResourceEntry[]>(rva, absoluteRva, list.ToArray());
                }

                return offsetToEntries;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //LowId
            sizeof(int) + //HighId
            sizeof(int); //OffsetToEntries

        private readonly MemoryChunk chunk;
        private readonly int rootRVA;

        internal MessageResourceBlock(in MemoryChunk chunk, int rootRVA)
        {
            this.chunk = chunk;
            this.rootRVA = rootRVA;

            offsetToEntries = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(OffsetToEntries, OffsetToEntriesOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.MESSAGE_RESOURCE_BLOCK, this, ViewKind.MessageResourceBlock, StructSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(LowId), LowIdOffset, LowId);
                    break;

                case 1:
                    structWriter.WriteField(nameof(HighId), HighIdOffset, HighId);
                    break;

                case 2:
                    structWriter.WriteRVAField(nameof(OffsetToEntries), OffsetToEntriesOffset, OffsetToEntries);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
