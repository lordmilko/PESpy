using System.Collections.Generic;
using System;
using System.Diagnostics;
using PESpy.View;
using System.Collections;
using System.Linq;

namespace PESpy
{
    internal class GuardLongJumpTargetTableDebugView
    {
        private GuardLongJumpTargetTable table;

        public GuardLongJumpTargetTableDebugView(GuardLongJumpTargetTable table)
        {
            this.table = table;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public GuardLongJumpTargetTable.Entry[] Items => table.ToArray();
    }

    public readonly struct GuardLongJumpTargetTable : IValue, IViewable, IEnumerable<GuardLongJumpTargetTable.Entry>
    {
        public int Count { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize => Count * (sizeof(int) + metadataSize);

        private readonly MemoryChunk chunk;
        private readonly byte metadataSize;

        internal GuardLongJumpTargetTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            this.chunk = chunk;

            Count = (int) entryCount;

            //See GuardCFFunctionTable for info
            var metadataSize = (int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT;
        }

        public Entry this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var entry = new Entry(chunk.Slice(index * (sizeof(int) + metadataSize)), metadataSize);

                return entry;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, metadataSize, chunk);

        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GuardLongJumpTargetTable, this, ViewKind.GuardLongJumpTargetTable, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline(this);

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

        public struct Enumerator : IEnumerator<Entry>
        {
            private readonly MemoryChunk chunk;
            private int index;
            private readonly int count;
            private readonly int metadataSize;

            internal Enumerator(int count, int metadataSize, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                this.metadataSize = metadataSize;
                index = default;
            }

            public Entry Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index < count)
                {
                    Current = new Entry(chunk.Slice(index * (sizeof(int) + metadataSize)), metadataSize);
                    index++;
                    return true;
                }

                Current = default;
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
