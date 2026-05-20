using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ClrDebug.PDB;
using PESpy.PDB;

namespace PESpy
{
    interface ISymbolMemoryBlock
    {
        HashSet<long> SymbolMemory { get; }
    }

    internal struct SymbolMemoryRange
    {
        public readonly long Start;
        public readonly long End;
        public readonly ICodeViewAccessor CodeViewAccessor;
        public readonly ICodeViewModuleAccessor? CodeViewModuleAccessor;

        internal SymbolMemoryRange(
            long start,
            long end,
            ICodeViewAccessor codeViewAccessor,
            ICodeViewModuleAccessor? codeViewModuleAccessor)
        {
            Start = start;
            End = end;
            CodeViewAccessor = codeViewAccessor;
            CodeViewModuleAccessor = codeViewModuleAccessor;
        }
    }

    public class SymbolMemoryTracker
    {
        /* We provide access to symbols directly from memory (either from the MMF or from a buffer they are copied into (when they span multiple pages).
         * As such, these pointers cannot contain any state, which presents a problem when they want to display strings (which may or may not
         * be length prefixed based on our PDBIMPV). As such, any time symbols are requested, the backing memory range will be added to this global
         * list. Idealy, it should be sorted so we can do a binary search on it, but for now there's no sorting */
        private static readonly List<SymbolMemoryRange> globalAccessorRanges = new();
        private static readonly ReaderWriterLockSlim globalMemoryRangesLock = new ReaderWriterLockSlim();

        internal static unsafe void RegisterPDBSymbolMemory(in MemoryChunk chunk, ICodeViewModuleAccessor? codeViewModuleAccessor)
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

                    InsertEntry(block, pdb!, codeViewModuleAccessor);
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        internal static unsafe void RegisterPDBSymbolMemory(
            PDBGlobalMemoryBlock globalBlock,
            byte* memory,
            int length,
            ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            var rangeOwner = (ISymbolMemoryBlock) globalBlock;

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                if (rangeOwner.SymbolMemory.Add((long) memory))
                {
                    //Its a PDB. We use ST strings if our version <= vc98
                    var pdb = globalBlock.PDBFile!;

                    InsertEntry(memory, length, pdb, codeViewModuleAccessor);
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        internal static unsafe void RegisterCVSymbolMemory(
            in MemoryChunk chunk,
            ICodeViewAccessor codeViewAccessor,
            ICodeViewModuleAccessor? codeViewModuleAccessor)
        {
            var block = chunk.block;
            var rangeOwner = (ISymbolMemoryBlock) block;

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                if (rangeOwner.SymbolMemory.Add((long) block.LocalPointer))
                {
                    //C13 uses UTF8; C7 and C11 use length prefixed. Not sure about C6

                    InsertEntry(block, codeViewAccessor, codeViewModuleAccessor);
                }
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        //This should only be used by unit tests, because we don't track whether a given address has been added yet
        internal static unsafe void RegisterSymbolMemory(
            byte* memory,
            int length,
            ICodeViewAccessor codeViewAccessor,
            ICodeViewModuleAccessor codeViewModuleAccessor)
        {
            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                InsertEntry(memory, length, codeViewAccessor, codeViewModuleAccessor);
            }
            finally
            {
                globalMemoryRangesLock.ExitWriteLock();
            }
        }

        private static unsafe void InsertEntry(MemoryBlock block, ICodeViewAccessor value, ICodeViewModuleAccessor? codeViewModuleAccessor) =>
            InsertEntry(block.LocalPointer, checked((int) block.Length), value, codeViewModuleAccessor);

        private static unsafe void InsertEntry(
            byte* memory,
            int length,
            ICodeViewAccessor codeViewAccessor,
            ICodeViewModuleAccessor? codeViewModuleAccessor)
        {
            var start = (long) memory;

            var list = globalAccessorRanges;

            var lo = 0;
            var hi = list.Count - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                if (list[mid].Start < start)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            list.Insert(lo, new SymbolMemoryRange(start, (long) (memory + length), codeViewAccessor, codeViewModuleAccessor));
        }

        internal static ImageSectionHeader[]? GetSectionHeaders(long address) => FindRange(address)?.CodeViewAccessor.GetSectionHeaders();

        internal unsafe static long GetStart(SymType symType) => GetStart((long) (SYMTYPE*) symType);

        internal static long GetStart(long address) => FindRange(address)?.Start ?? 0;

        internal static ICodeViewAccessor? GetAccessor(long address) => FindRange(address)?.CodeViewAccessor;

        internal static ICodeViewModuleAccessor? GetModuleAccessor(long address) => FindRange(address)?.CodeViewModuleAccessor;

        internal static bool IsLengthPrefixedData(long address) => FindRange(address)?.CodeViewAccessor.HasLengthPrefixedStrings ?? false;

        internal static SymbolMemoryRange? FindRange(long address)
        {
            if (address == 0)
                throw new InvalidOperationException("Cannot search for a value with address 0");

            globalMemoryRangesLock.EnterReadLock();

            try
            {
                //We ensure our ranges are sorted; we should be able to binary search

                var list = globalAccessorRanges;

                var low = 0;
                var high = list.Count - 1;

                while (low <= high)
                {
                    var mid = low + (high - low) / 2;
                    var item = list[mid];

                    if (address >= item.Start)
                    {
                        if (address <= item.End)
                        {
                            //It's a match
                            return item;
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

            //temp
            //Debug.Assert(false, "Attempted to query data in an unregistered memory address");
            return default; //Assume it's a modern file with non-length prefixed strings
        }

        internal static void ClearSymbolMemory(ISymbolMemoryBlock block)
        {
            if (Environment.HasShutdownStarted)
                return; //Don't bother cleaning up

            globalMemoryRangesLock.EnterWriteLock();

            try
            {
                globalAccessorRanges.RemoveAll(v => block.SymbolMemory.Contains(v.Start));
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
                globalAccessorRanges.RemoveAll(v => v.Start == (long) memory);
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
