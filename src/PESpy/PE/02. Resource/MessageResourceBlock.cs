using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("0x{LowId.ToString(\"X\"),nq} - 0x{HighId.ToString(\"X\"),nq}")]
    public struct MessageResourceBlock : IValue, IViewable
    {
        private const int OffsetToEntriesOffset = 8;

        //Values are regularly uints
        public uint LowId => chunk.PeekUInt32(0);

        public uint HighId => chunk.PeekUInt32(4);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(LowId), LowId);
            s.WriteField(nameof(HighId), HighId);
            s.WriteRVAField(nameof(OffsetToEntries), OffsetToEntries);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
