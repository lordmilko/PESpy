using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct GuardEHContinuationTable : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entry[] Entries { get; }

        public int Offset { get; }

#if PEFAST
        internal GuardEHContinuationTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            Offset = chunk.AbsoluteOffset;

            //Eagerly populate. If you're asking for the GuardEHContinuationTable, you want the entries

            //See GuardCFFunctionTable for info
            //https://windows-internals.com/cet-on-windows/
            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;

            var entries = new Entry[entryCount];

            var read = 0;

            for (var i = 0; i < entryCount; i++)
            {
                entries[i] = new Entry(chunk.Slice(read), metadataSize);
                read += 4 + metadataSize;
            }

            Entries = entries;
        }
#else
        internal GuardEHContinuationTable(IFileReader reader, IMAGE_GUARD flags, long entryCount)
        {
            Offset = (int) reader.Position;

            //See GuardCFFunctionTable for info
            //https://windows-internals.com/cet-on-windows/
            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;

            reader.FillBuffer((int) ((4 + metadataSize) * entryCount));

            var entries = new Entry[entryCount];

            for (var i = 0; i < entryCount; i++)
                entries[i] = new Entry(reader, metadataSize);

            Entries = entries;
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(GuardEHContinuationTable), this, ViewKind.GuardEHContinuationTable);

            s.WriteInline(Entries);
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"Function = 0x{Function:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";

            public int Function { get; init; }

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public int Offset { get; init; }

#if PEFAST
            internal Entry(in MemoryChunk chunk, int metadataSize)
            {
                Offset = chunk.AbsoluteOffset;

                Function = chunk.PeekInt32(0);

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
#else
            internal Entry(IFileReader reader, int metadataSize)
            {
                Offset = (int) reader.Position;

                Function = reader.ReadInt32();

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
#endif

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct("EHCONT Entry", this, ViewKind.GuardEHContinuationTable_Entry);

                s.WriteField(nameof(Function), Function);

                if (Flags != null)
                    s.WriteField(nameof(Flags), Flags.Value, sizeof(byte));
            }
        }
    }
}
