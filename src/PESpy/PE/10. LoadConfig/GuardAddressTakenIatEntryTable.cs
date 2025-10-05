using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class GuardAddressTakenIatEntryTableDebugView
    {
        private GuardAddressTakenIatEntryTable table;

        public GuardAddressTakenIatEntryTableDebugView(GuardAddressTakenIatEntryTable table)
        {
            this.table = table;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public GuardAddressTakenIatEntryTable.Entry[] Items => table.ToArray();
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(GuardCFFunctionTableDebugView))]
    public readonly struct GuardAddressTakenIatEntryTable : IValue, IViewable, IEnumerable<GuardAddressTakenIatEntryTable.Entry>
    {
        public int Count { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize => Count * (sizeof(int) + metadataSize);

        private readonly MemoryChunk chunk;
        private readonly byte metadataSize;

        internal GuardAddressTakenIatEntryTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            this.chunk = chunk;

            Count = (int) entryCount;

            //See GuardCFFunctionTable for info
            metadataSize = (byte) ((int) (flags & IMAGE_GUARD.CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT);
        }

        public Entry this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var peFile = chunk.PEFile();
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
            writer.NewStruct(Strings.GuardAddressTakenIatEntryTable, this, ViewKind.GuardAddressTakenIatEntryTable, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline<GuardAddressTakenIatEntryTable, Entry>(this);

            return s.ToArray();
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public readonly struct Entry : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    if (TryGetImportInfo(out var descriptorIndex, out var thunkIndex, out var isDelayImport))
                    {
                        if (isDelayImport)
                        {
                            ref var descriptor = ref peFile.DelayImportTable![descriptorIndex];
                            ref var thunk = ref descriptor.ImportNameTableRVA.Value[thunkIndex];

                            return $"[Delay] {descriptor.DllNameRVA} {thunk}";
                        }
                        else
                        {
                            ref var descriptor = ref peFile.ImportTable![descriptorIndex];
                            ref var thunk = ref descriptor.OriginalFirstThunk.Value[thunkIndex];

                            return $"[Import] {descriptor.Name} {thunk}";
                        }
                    }

                    return $"Function = 0x{Function:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}";
                }
            }

            public int Function { get; init; }

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public int Offset { get; }

            private readonly PEFile peFile;

            internal Entry(in MemoryChunk chunk, int metadataSize)
            {
                Offset = chunk.AbsoluteOffset;
                this.peFile = chunk.PEFile();

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

            public bool TryGetImportInfo(out int descriptorIndex, out int thunkIndex, out bool isDelayImport)
            {
                var targetAddress = Function;

                if (!peFile.IsLoadedImage)
                {
                    if (!peFile.TryGetOffset(Function, out targetAddress))
                    {
                        descriptorIndex = default;
                        thunkIndex = default;
                        isDelayImport = default;
                        return false;
                    }
                }

                var importTable = peFile.ImportTable;

                if (importTable != null)
                {
                    if (TryGetImportTableInfo(targetAddress, importTable, out descriptorIndex, out thunkIndex))
                    {
                        isDelayImport = false;
                        return true;
                    }
                }

                var delayImportTable = peFile.DelayImportTable;

                if (delayImportTable != null)
                {
                    if (TryGetDelayImportTableInfo(targetAddress, delayImportTable, out descriptorIndex, out thunkIndex))
                    {
                        isDelayImport = true;
                        return true;
                    }
                }

                descriptorIndex = default;
                thunkIndex = default;
                isDelayImport = default;
                return false;
            }

            private static bool TryGetImportTableInfo(int targetAddress, ImageImportDescriptor[] importTable, out int descriptorIndex, out int thunkIndex)
            {
                for (var i = 0; i < importTable.Length; i++)
                {
                    ref var descriptor = ref importTable[i];

                    var iat = descriptor.FirstThunk;

                    if (iat.IsValid)
                    {
                        if (TrySearchThunkList(targetAddress, iat.Value, out thunkIndex))
                        {
                            descriptorIndex = i;
                            return true;
                        }
                    }
                }

                descriptorIndex = default;
                thunkIndex = default;
                return false;
            }

            private static bool TryGetDelayImportTableInfo(int targetAddress, ImageDelayLoadDescriptor[] importTable, out int descriptorIndex, out int thunkIndex)
            {
                for (var i = 0; i < importTable.Length; i++)
                {
                    ref var descriptor = ref importTable[i];

                    var iat = descriptor.ImportAddressTableRVA;

                    if (iat.IsValid)
                    {
                        if (TrySearchThunkList(targetAddress, iat.Value, out thunkIndex))
                        {
                            descriptorIndex = i;
                            return true;
                        }
                    }
                }

                descriptorIndex = default;
                thunkIndex = default;
                return false;
            }

            private static bool TrySearchThunkList(int targetAddress, ImageThunkData[] thunkList, out int index)
            {
                if (thunkList.Length == 0)
                {
                    index = -1;
                    return false;
                }

                if (targetAddress < thunkList[0].Offset || targetAddress > thunkList[thunkList.Length - 1].Offset)
                {
                    index = -1;
                    return false;
                }

                var lo = 0;
                var hi = thunkList.Length - 1;

                while (lo <= hi)
                {
                    var mid = (lo + hi) / 2;

                    var offset = thunkList[mid].Offset;

                    if (offset > targetAddress)
                        hi = mid - 1;
                    else if (offset < targetAddress)
                        lo = mid + 1;
                    else
                    {
                        index = mid;
                        return true;
                    }
                }

                //Something went wrong. PE File is corrupt?
                index = -1;
                return false;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.Entry, this, ViewKind.GuardAddressTakenIatEntryTable_Entry, sizeof(int) + (Flags != null ? 1 : 0));

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField(nameof(Function), Function);

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
