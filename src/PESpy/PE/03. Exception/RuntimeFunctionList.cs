using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class RuntimeFunctionListDebugView
    {
        private RuntimeFunctionList runtimeFunctionList;

        public RuntimeFunctionListDebugView(RuntimeFunctionList runtimeFunctionList)
        {
            this.runtimeFunctionList = runtimeFunctionList;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RuntimeFunction[] Items => runtimeFunctionList.ToArray();
    }

    /// <summary>
    /// Provides access to <see cref="RuntimeFunction"/> instances without allocating an array.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    [DebuggerTypeProxy(typeof(RuntimeFunctionListDebugView))]
    public class RuntimeFunctionList : IEnumerable<RuntimeFunction>, ILightweightList<RuntimeFunctionList.Enumerator, RuntimeFunction> //Must be a class to denote that the ExceptionTable is missing
    {
        private string DebuggerDisplay() => chunk.block == null ? "null" : $"Count = {Count}";

        internal readonly MemoryChunk chunk;

        public int Count { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal RuntimeFunctionList(int count, in MemoryChunk chunk)
        {
            Count = count;
            this.chunk = chunk;
        }

        public RuntimeFunction this[int index]
        {
            get
            {
                if (chunk.block == null)
                    throw new NullReferenceException();

                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                var value = new RuntimeFunction(chunk.Slice(index * RuntimeFunction.StructSize));

                return value;
            }
        }

        /// <summary>
        /// Tries to find the <see cref="RuntimeFunction"/> whose bounds a given RVA
        /// lies within.
        /// </summary>
        /// <param name="rva">The RVA to search for.</param>
        /// <param name="runtimeFunction">The <see cref="RuntimeFunction"/> whose begin and end address enclose the specified RVA.</param>
        /// <returns>True if an associated entry was found, otherwise false.</returns>
        public unsafe bool TryFindEntry(long rva, out RuntimeFunction runtimeFunction)
        {
            var pRuntimeFunction = (RUNTIME_FUNCTION*) chunk.Pointer;

            var lo = 0;
            var hi = Count - 1;

            //Based on the logic employed by DbgHelp
            while (hi >= lo)
            {
                var mid = (lo + hi) >> 1;

                var entry = pRuntimeFunction + mid;

                if (rva < entry->BeginAddress)
                    hi = mid - 1;
                else if (rva >= entry->EndAddress)
                    lo = mid + 1;
                else
                {
                    runtimeFunction = new RuntimeFunction(chunk.Slice(mid * RuntimeFunction.StructSize));
                    return true;
                }
            }

            runtimeFunction = default;
            return false;
        }

        internal unsafe void WriteFast(ViewWriter writer)
        {
            //Writing items normally is super slow in msedge.dll, so we want to try and optimize for RUNTIME_FUNCTION
            //which can have a huge number of elements
            if (writer is PEViewByteViewWriter w)
            {
                var pRuntimeFunction = (RUNTIME_FUNCTION*) chunk.Pointer;

                var elementSize = sizeof(RUNTIME_FUNCTION);
                var pEnd = pRuntimeFunction + Count;

                var offset = Offset;

                var unwindInfoStart = 0;
                var unwindInfoEnd = 0;
                var adjustment = 0;

                var peFile = chunk.PEFile();

                var exceptionTableDirectory = peFile.OptionalHeader.ExceptionTableDirectory;
                var exceptionTableStart = exceptionTableDirectory.VirtualAddress;
                var exceptionTableEnd = exceptionTableStart + exceptionTableDirectory.Size;

                for (var i = pRuntimeFunction; i < pEnd; i++, offset += elementSize)
                {
                    w.NewStruct(offset, elementSize, ViewKind.RuntimeFunction);

                    w.WriteRVAXRef(offset, RuntimeFunction.BeginAddressOffset, i->BeginAddress);
                    w.WriteRVAXRef(offset, RuntimeFunction.EndAddressOffset, i->EndAddress);

                    var unwindData = i->UnwindData;

                    if (unwindData == 0)
                        continue;

                    //We need to establish an xref between this RUNTIME_FUNCTION and this UNWIND_INFO.
                    //We will opportunistically try and assume that all UNWIND_INFO items will be in the same section
                    if (unwindInfoStart == 0)
                    {
                        if (unwindData >= exceptionTableStart && unwindData < exceptionTableEnd)
                            continue; //This one points inside a RUNTIME_FUNCTION entry!

                        if (!peFile.TryGetSectionContainingRVA(unwindData, out var index, out var header))
                            continue; //It wasn't valid anyway

                        unwindInfoStart = header.VirtualAddress;
                        unwindInfoEnd = unwindInfoStart + header.VirtualSize;

                        adjustment = peFile.IsLoadedImage
                            ? 0
                            : header.PointerToRawData - header.VirtualAddress;
                    }

                    if (unwindData >= unwindInfoStart && unwindData < unwindInfoEnd)
                    {
                        //Easy case: it's within the area we expect
                        var actualOffset = unwindData + adjustment;
                        w.WriteOffsetXRef(offset, RuntimeFunction.UnwindDataOffset, actualOffset);
                        w.RecordUnwindInfo(unwindData);
                    }
                    else
                    {
                        //Exclude items that point 1 byte inside other RUNTIME_FUNCTION items
                        if (unwindData >= exceptionTableStart && unwindData < exceptionTableEnd)
                            continue; //This one points inside a RUNTIME_FUNCTION entry!

                        //Hard case: the value was in some other weird section. We need to look it up manually
                        if (!peFile.TryGetSectionContainingRVA(unwindData, out var index, out var header))
                            continue; //It wasn't valid anyway

                        //Implicitly we're within bounds since we just looked up the section we belong to
                        var actualOffset = peFile.IsLoadedImage
                            ? unwindData
                            : header.PointerToRawData - header.VirtualAddress + unwindData;

                        w.WriteOffsetXRef(offset, RuntimeFunction.UnwindDataOffset, actualOffset);
                    }
                }
            }
            else
                writer.WriteGlobal<RuntimeFunctionList, Enumerator, RuntimeFunction>(this);
        }

        public override bool Equals(object obj) => this == obj;

        public override int GetHashCode() => chunk.block.GetHashCode();

        public Enumerator GetEnumerator() => new Enumerator(Count, chunk);

        IEnumerator<RuntimeFunction> IEnumerable<RuntimeFunction>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<RuntimeFunction>
        {
            private readonly MemoryChunk chunk;
            private int index;
            private readonly int count;

            internal Enumerator(int count, in MemoryChunk chunk)
            {
                this.chunk = chunk;
                this.count = count;
                index = default;

                Current = default;
            }

            public RuntimeFunction Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (index < count)
                {
                    Current = new RuntimeFunction(chunk.Slice(index * RuntimeFunction.StructSize));
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
