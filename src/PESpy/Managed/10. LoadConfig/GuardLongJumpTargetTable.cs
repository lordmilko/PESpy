using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct GuardLongJumpTargetTable : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entry[] Entries { get; }

        public int Offset { get; }

        internal GuardLongJumpTargetTable(IFileReader reader, IMAGE_GUARD flags, long entryCount)
        {
            Offset = (int) reader.Position;

            //See GuardCFFunctionTable for info
            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;

            reader.FillBuffer((int) ((4 + metadataSize) * entryCount));

            var entries = new Entry[entryCount];

            for (var i = 0; i < entryCount; i++)
                entries[i] = new Entry(reader, metadataSize);

            Entries = entries;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(GuardLongJumpTargetTable), this, ViewKind.GuardLongJumpTargetTable);

            s.WriteInline(Entries);
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"Target = 0x{Target:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";

            public int Target { get; init; } //RVA of the target of the jump

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public int Offset { get; init; }

            internal Entry(IFileReader reader, int metadataSize)
            {
                Offset = (int)reader.Position;

                Target = reader.ReadInt32();

                switch (metadataSize)
                {
                    case 0:
                        Flags = null;
                        break;

                    case 1:
                        Flags = (IMAGE_GUARD_FLAG) reader.ReadByte();
                        break;

                    default:
                        Debug.Assert(false, $"Don't know how to handle a GFIDS entry of size {metadataSize}");
                        Flags = null;
                        break;
                }
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct("Entry", this, ViewKind.GuardLongJumpTargetTable_Entry);
                s.WriteField(nameof(Target), Target);

                if (Flags != null)
                    s.WriteField(nameof(Flags), Flags.Value, sizeof(byte));
            }
        }
    }
}
