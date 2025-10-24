using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class GuardEHContinuationTableDebugView
    {
        private GuardEHContinuationTable table;

        public GuardEHContinuationTableDebugView(GuardEHContinuationTable table)
        {
            this.table = table;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public GuardEHContinuationTable.Entry[] Items => table.ToArray();
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GuardEHContinuationTableDebugView))]
    public readonly struct GuardEHContinuationTable : IValue, IViewable, IEnumerable<GuardEHContinuationTable.Entry>
    {
        public int Count { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize => Count * (sizeof(int) + metadataSize);

        private readonly MemoryChunk chunk;
        private readonly byte metadataSize;

        internal GuardEHContinuationTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            this.chunk = chunk;

            Count = (int) entryCount;

            //See GuardCFFunctionTable for info
            //https://windows-internals.com/cet-on-windows/
            metadataSize = (byte) ((int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT);
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
            writer.NewStruct(Strings.GuardEHContinuationTable, this, ViewKind.GuardEHContinuationTable, StructSize);

        int IViewable.NumChildren() => Count;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.WriteInline(this[index]);
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            private const int FunctionOffset = 0;
            private const int FlagsOffset = 4;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay => $"Function = 0x{Function:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";

            public int Function { get; init; }

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public int Offset { get; init; }

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

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.EHCONTEntry, this, ViewKind.GuardEHContinuationTable_Entry, sizeof(int) + (Flags != null ? 1 : 0));

            int IViewable.NumChildren() => Flags == null ? 1 : 2;

            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 0:
                        structWriter.WriteField(nameof(Function), FunctionOffset, Function);
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

            internal Enumerator(int count, int metadataSize, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                this.metadataSize = metadataSize;
                index = default;

                Current = default;
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
