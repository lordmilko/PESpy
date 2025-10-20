using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace PESpy
{
    interface ISymbolMemoryBlock
    {
        HashSet<long> SymbolMemory { get; }
    }

    public class SymbolMemoryTracker
    {
        /* We provide access to symbols directly from memory (either from the MMF or from a buffer they are copied into (when they span multiple pages).
         * As such, these pointers cannot contain any state, which presents a problem when they want to display strings (which may or may not
         * be length prefixed based on our PDBIMPV). As such, any time symbols are requested, the backing memory range will be added to this global
         * list. Idealy, it should be sorted so we can do a binary search on it, but for now there's no sorting */
        private static readonly List<(long start, long end, ISymbolAccessor? file)> globalAccessorRanges = new();
        private static readonly ReaderWriterLockSlim globalMemoryRangesLock = new ReaderWriterLockSlim();

        internal static unsafe void RegisterPDBSymbolMemory(in MemoryChunk chunk)
        {
            var block = chunk.block;
            var rangeOwner = (ISymbolMemoryBlock) block;

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                if (rangeOwner.SymbolMemory.Add((long) block.LocalPointer))
                {
                    //Its a PDB. We use ST strings if our version <= vc98
                    var pdb = ((PagedMemoryBlock) block).PDBFile;

                    InsertEntry(block, globalAccessorRanges, pdb);
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        internal static unsafe void RegisterPDBSymbolMemory(PDBGlobalMemoryBlock globalBlock, byte* memory, int length)
        {
            var rangeOwner = (ISymbolMemoryBlock) globalBlock;

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                if (rangeOwner.SymbolMemory.Add((long) memory))
                {
                    //Its a PDB. We use ST strings if our version <= vc98
                    var pdb = globalBlock.PDBFile;

                    InsertEntry(memory, length, globalAccessorRanges, pdb);
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        internal static unsafe void RegisterCVSymbolMemory(in MemoryChunk chunk, ISymbolAccessor symbolAccessor)
        {
            var block = chunk.block;
            var rangeOwner = (ISymbolMemoryBlock) block;

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                if (rangeOwner.SymbolMemory.Add((long) block.LocalPointer))
                {
                    //C13 uses UTF8; C7 and C11 use length prefixed. Not sure about C6

                    InsertEntry(block, globalAccessorRanges, symbolAccessor);
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        //This should only be used by unit tests, because we don't track whether a given address has been added yet
        internal static unsafe void RegisterSymbolMemory(byte* memory, int length, ISymbolAccessor symbolAccessor)
        {
            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                InsertEntry(memory, length, globalAccessorRanges, symbolAccessor);
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        private static unsafe void InsertEntry<T>(MemoryBlock block, List<(long start, long end, T value)> list, T value) =>
            InsertEntry<T>(block.LocalPointer, block.Length, list, value);

        private static unsafe void InsertEntry<T>(byte* memory, int length, List<(long start, long end, T value)> list, T value)
        {
            var start = (long) memory;

            var lo = 0;
            var hi = list.Count - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                if (list[mid].start < start)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            list.Insert(lo, (start, (long) (memory + length), value));
        }

        internal static ImageSectionHeader[]? GetSectionHeaders(long address)
        {
            var accessor = FindItem(address, globalAccessorRanges, out _);

            if (accessor == null)
                return null;

            return accessor.GetSectionHeaders();
        }

        internal static long GetStart(long address)
        {
            FindItem(address, globalAccessorRanges, out var start);
            return start;
        }

        internal static ISymbolAccessor? GetAccessor(long address) => FindItem(address, globalAccessorRanges, out _);

        internal static bool IsLengthPrefixedData(long address) => FindItem(address, globalAccessorRanges, out _)?.HasLengthPrefixedStrings ?? false;

        private static ISymbolAccessor? FindItem(
            long address,
            List<(long start, long end, ISymbolAccessor? value)> list,
            out long start)
        {
            if (address == 0)
                throw new InvalidOperationException("Cannot search for a value with address 0");

            globalMemoryRangesLock.EnterReadLock();

            try
            {
                //We ensure our ranges are sorted; we should be able to binary search

                var low = 0;
                var high = list.Count - 1;

                while (low <= high)
                {
                    var mid = low + (high - low) / 2;
                    var item = list[mid];

                    if (address >= item.start)
                    {
                        if (address <= item.end)
                        {
                            //It's a match
                            start = item.start;
                            return item.value;
                        }
                        else
                        {
                            low = mid + 1;
                        }
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitReadLock();
            }

            Debug.Assert(false, "Attempted to query data in an unregistered memory address");
            start = default;
            return default; //Assume it's a modern file with non-length prefixed strings
        }

        internal static void ClearSymbolMemory(ISymbolMemoryBlock block)
        {
            if (Environment.HasShutdownStarted)
                return; //Don't bother cleaning up

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                globalAccessorRanges.RemoveAll(v => block.SymbolMemory.Contains(v.start));
                block.SymbolMemory.Clear();
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        //This should only be used by unit tests
        internal static unsafe void ClearSymbolMemory(byte* memory)
        {
            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                globalAccessorRanges.RemoveAll(v => v.start == (long) memory);
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        internal static void AssertNoRanges()
        {
            Debug.Assert(globalAccessorRanges.Count == 0, "All accessor ranges have not been cleaned up");
        }
    }
}
