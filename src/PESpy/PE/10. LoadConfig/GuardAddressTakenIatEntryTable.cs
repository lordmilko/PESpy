using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
    [DebuggerTypeProxy(typeof(GuardAddressTakenIatEntryTableDebugView))]
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
            metadataSize = (byte) ((int) (flags & IMAGE_GUARD.IMAGE_GUARD_CF_FUNCTION_TABLE_SIZE_MASK) >> ImageLoadConfigDirectory.CF_FUNCTION_TABLE_SIZE_SHIFT);
        }

        public Entry this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var peFile = chunk.PEFile();
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
            writer.NewStruct(Strings.GuardAddressTakenIatEntryTable, this, ViewKind.GuardAddressTakenIatEntryTable, StructSize);

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
            private string DebuggerDisplay
            {
                get
                {
                    if (TryGetImportInfo(out var descriptorIndex, out var thunkIndex, out var isDelayImport))
                    {
                        if (isDelayImport)
                        {
                            ref var descriptor = ref peFile.DelayImportTable![descriptorIndex];
                            var thunk = descriptor.ImportNameTableRVA.Value[thunkIndex];

                            return $"[Delay] {descriptor.DllNameRVA} {thunk}";
                        }
                        else
                        {
                            ref var descriptor = ref peFile.ImportTable![descriptorIndex];
                            var thunk = descriptor.OriginalFirstThunk.Value[thunkIndex];

                            return $"[Import] {descriptor.Name} {thunk}";
                        }
                    }

                    var symbolAccessor = peFile.GetSymbolAccessor(LocatorHttpPolicy.None);

                    var builder = new StringBuilder();
                    builder.Append($"Function = 0x{Function:X}, Flags = {(Flags.HasValue ? Flags.Value.ToString() : "null")}");

                    if (symbolAccessor is not NullSymbolAccessor)
                    {
                        builder.Append(", Symbol = ");

                        if (symbolAccessor.TryGetNameFromAddress(Function, out var name, out var displacement))
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
            }

            public int Function { get; init; }

            public IMAGE_GUARD_FLAG? Flags { get; init; }

            public int Offset { get; }

            private readonly PEFile peFile;

            internal Entry(in MemoryChunk chunk, int metadataSize, PEFile peFile)
            {
                Offset = chunk.AbsoluteOffset;
                this.peFile = peFile;

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

            private static bool TrySearchThunkList(int targetAddress, ImageThunkDataList thunkList, out int index)
            {
                if (thunkList.Count == 0)
                {
                    index = -1;
                    return false;
                }

                if (targetAddress < thunkList[0].Offset || targetAddress > thunkList[thunkList.Count - 1].Offset)
                {
                    index = -1;
                    return false;
                }

                var lo = 0;
                var hi = thunkList.Count - 1;

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
                writer.WriteRVAXRef(Offset, FunctionOffset, Function);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.Entry, this, ViewKind.GuardAddressTakenIatEntryTable_Entry, sizeof(int) + (Flags != null ? 1 : 0));

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
            private readonly PEFile peFile;

            internal Enumerator(int count, int metadataSize, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                this.metadataSize = metadataSize;
                index = default;
                this.peFile = chunk.PEFile();

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
