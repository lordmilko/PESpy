using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct GuardAddressTakenIatEntryTable : IValue, IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entry[] Entries { get; }

        public int Offset { get; }

        private readonly MemoryChunk chunk;
        private readonly int length;

        internal GuardAddressTakenIatEntryTable(in MemoryChunk chunk, IMAGE_GUARD flags, long entryCount)
        {
            this.chunk = chunk;

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

            //We either need to store length or metadataSize to calculate the IView size, and using metadataSize will mean
            //we need to also multiply by the number of entries and do 4 + metadataSize for each item. May as well just store
            //length
            this.length = read;

            Entries = entries;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GuardAddressTakenIatEntryTable, this, ViewKind.GuardAddressTakenIatEntryTable, length);

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
    }
}
