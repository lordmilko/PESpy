using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct GuardLongJumpTargetTable : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entry[] Entries { get; }

        public int Offset { get; }

        private readonly int length;

        internal GuardLongJumpTargetTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            Offset = (int) chunk.AbsoluteOffset;

            //See GuardCFFunctionTable for info
            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;

            var entries = new Entry[entryCount];

            var read = 0;

            for (var i = 0; i < entryCount; i++)
            {
                entries[i] = new Entry(chunk.Slice(read), metadataSize);
                read += 4 + metadataSize;
            }

            length = read;

            Entries = entries;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GuardLongJumpTargetTable, this, ViewKind.GuardLongJumpTargetTable, length);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline(Entries);

            return s.ToArray();
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"Target = 0x{Target:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";

            public int Target { get; init; } //RVA of the target of the jump

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public int Offset { get; init; }

            internal Entry(in MemoryChunk chunk, int metadataSize)
            {
                Offset = (int) chunk.AbsoluteOffset;

                Target = chunk.PeekInt32(0);

                switch (metadataSize)
                {
                    case 0:
                        Flags = null;
                        break;

                    case 1:
                        Flags = (IMAGE_GUARD_FLAG) chunk.PeekByte(4);
                        break;

                    default:
                        Debug.Assert(false, $"Don't know how to handle a GFIDS entry of size {metadataSize}");
                        Flags = null;
                        break;
                }
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.Entry, this, ViewKind.GuardLongJumpTargetTable_Entry, sizeof(int) + (Flags != null ? 1 : 0));

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

                s.WriteField(nameof(Target), Target);

                if (Flags != null)
                    s.WriteField(nameof(Flags), Flags.Value, sizeof(byte));

                return s.ToArray();
            }
        }
    }
}
