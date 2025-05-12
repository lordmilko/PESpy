using System.Collections.Generic;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    interface ISymbolMemoryBlock
    {
        HashSet<long> SymbolMemory { get; }
    }

    class SymbolMemoryTracker
    {
        /* We provide access to symbols directly from memory (either from the MMF or from a buffer they are copied into (when they span multiple pages).
         * As such, these pointers cannot contain any state, which presents a problem when they want to display strings (which may or may not
         * be length prefixed based on our PDBIMPV). As such, any time symbols are requested, the backing memory range will be added to this global
         * list. Idealy, it should be sorted so we can do a binary search on it, but for now there's no sorting */
        private static readonly List<(long start, long end, bool isLengthPrefixedString)> globalMemoryRanges = new();
        private static readonly List<(long start, long end, PDBFile? pdb)> globalPdbRanges = new();
        private static readonly object globalMemoryRangesLock = new object();

        internal static unsafe void RegisterPDBSymbolMemory(in MemoryChunk chunk)
        {
            var block = chunk.block;
            var rangeOwner = (ISymbolMemoryBlock) block;

            lock (globalMemoryRangesLock)
            {
                if (rangeOwner.SymbolMemory.Add((long) block.LocalPointer))
                {
                    //Its a PDB. We use ST strings if our version <= vc98
                    var pdb = ((PagedMemoryBlock) block).PDBFile;

                    var isLengthPrefixedString = pdb.PDB!.PDBHeader.ImplementationVersion <= PDBIMPV.PDBImpvVC98;

                    InsertEntry(block, globalMemoryRanges, isLengthPrefixedString);
                    InsertEntry(block, globalPdbRanges, pdb);
                }
            }
        }

        internal static unsafe void RegisterCVSymbolMemory(CV_SIGNATURE signature, in MemoryChunk chunk)
        {
            var block = chunk.block;
            var rangeOwner = (ISymbolMemoryBlock) block;

            lock (globalMemoryRangesLock)
            {
                if (rangeOwner.SymbolMemory.Add((long) block.LocalPointer))
                {
                    //C13 uses UTF8; C7 and C11 use length prefixed. Not sure about C6
                    var isLengthPrefixedString = signature != CV_SIGNATURE.C13;

                    InsertEntry(block, globalMemoryRanges, isLengthPrefixedString);
                }
            }
        }

        private static unsafe void InsertEntry<T>(MemoryBlock block, List<(long start, long end, T value)> list, T value)
        {
            var start = (long) block.LocalPointer;

            var didInsert = false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].start > start)
                {
                    list.Insert(i, (start, (long) (block.LocalPointer + block.Length), value));
                    didInsert = true;
                    break;
                }
            }

            if (!didInsert)
                list.Add(((long) block.LocalPointer, (long) (block.LocalPointer + block.Length), value));
        }

        internal static ImageSectionHeader[]? GetSectionHeaders(long address)
        {
            var dbi = FindItem(address, globalPdbRanges)?.DBI;

            return dbi?.SectionHdr;
        }

        internal static IModi[]? GetModules(long address)
        {
            var dbi = FindItem(address, globalPdbRanges)?.DBI;

            return dbi?.Modules;
        }

        internal static PDBFile? GetPDB(long address) => FindItem(address, globalPdbRanges);

        internal static bool IsLengthPrefixedData(long address) => FindItem(address, globalMemoryRanges);

        private static T? FindItem<T>(long address, List<(long start, long end, T value)> list)
        {
            lock (globalMemoryRangesLock)
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

            Debug.Assert(false, "Attempted to query whether data ian unregistered memory address");
            return default; //Assume it's a modern file with non-length prefixed strings
        }

        internal static void ClearSymbolMemory(ISymbolMemoryBlock block)
        {
            lock (globalMemoryRangesLock)
            {
                globalMemoryRanges.RemoveAll(v => block.SymbolMemory.Contains(v.start));
                globalPdbRanges.RemoveAll(v => block.SymbolMemory.Contains(v.start));
                block.SymbolMemory.Clear();
            }
        }
    }
}
