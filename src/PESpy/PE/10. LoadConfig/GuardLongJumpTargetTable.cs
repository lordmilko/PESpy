using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClrDebug;
using PESpy.View;

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

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GuardLongJumpTargetTableDebugView))]
    public readonly struct GuardLongJumpTargetTable : IValue, IViewable, IEnumerable<GuardLongJumpTargetTable.Entry>
    {
        public int Count { get; }

        public long Offset => chunk.AbsoluteOffset;

        internal int StructSize => Count * (sizeof(int) + metadataSize);

        private readonly MemoryChunk chunk;
        private readonly byte metadataSize;

        internal GuardLongJumpTargetTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            this.chunk = chunk;

            Count = (int) entryCount;

            //See GuardCFFunctionTable for info
            metadataSize = (byte) ((int) (flags & IMAGE_GUARD.IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT);
        }

        public Entry this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var entry = new Entry(chunk.Slice(index * (sizeof(int) + metadataSize)), metadataSize, chunk.PEFile());

                return entry;
            }
        }

        public unsafe bool TryGetEntry(int rva, out Entry entry)
        {
            if (ImageLoadConfigDirectory.TryGetGuardEntry(rva, metadataSize, Count, chunk.Pointer, out var offset))
            {
                entry = new Entry(chunk.Slice(offset), metadataSize, chunk.PEFile());
                return true;
            }

            entry = default;
            return false;
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, metadataSize, chunk);

        IEnumerator<Entry> IEnumerable<Entry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.GuardLongJumpTargetTable, StructSize);

        int IViewable.NumChildren() => Count;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.WriteInline(this[index]);
        }

        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            private const int TargetOffset = 0;
            private const int FlagsOffset = 4;

            private string DebuggerDisplay()
            {
                var symbolAccessor = peFile.GetSymbolAccessor(LocatorHttpPolicy.None);

                var builder = new StringBuilder();
                builder.Append($"Target = 0x{Target:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}");

                if (symbolAccessor is not NullSymbolAccessor)
                {
                    builder.Append(", Symbol = ");

                    if (symbolAccessor.TryGetNameFromAddress(Target, out var name, out var displacement))
                    {
                        builder.Append(name);

                        if (displacement != 0)
                            builder.Append("+0x").Append(displacement.ToString("X"));
                    }
                    else
                    {
                        builder.Append("?");
                    }
                }

                return builder.ToString();
            }

            public int Target { get; init; } //RVA of the target of the jump

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public long Offset { get; init; }

            private readonly PEFile peFile;

            internal Entry(in MemoryChunk chunk, int metadataSize, PEFile peFile)
            {
                Offset = chunk.AbsoluteOffset;
                this.peFile = peFile;

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
                writer.WriteRVAXRef(Offset, TargetOffset, Target);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(this, ViewKind.GuardLongJumpTargetTable_Entry, sizeof(int) + (Flags != null ? 1 : 0));

            int IViewable.NumChildren() => Flags == null ? 1 : 2;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(Target), TargetOffset, Target);
                        break;

                    case 1:
                        if (Flags != null)
                            structWriter.WriteField(nameof(Flags), FlagsOffset, Flags.Value, sizeof(byte));
                        else
                            throw new IndexOutOfRangeException();

                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public struct Enumerator : IEnumerator<Entry>
        {
            private readonly MemoryChunk chunk;
            private int index;
            private readonly int count;
            private readonly int metadataSize;
            private readonly PEFile peFile;

            internal Enumerator(int count, int metadataSize, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                this.metadataSize = metadataSize;
                index = default;
                peFile = chunk.PEFile();

                Current = default;
            }

            public Entry Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index < count)
                {
                    Current = new Entry(chunk.Slice(index * (sizeof(int) + metadataSize)), metadataSize, peFile);
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
