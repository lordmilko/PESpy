using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    internal class ModuleFileNamesListDebugView
    {
        private OMFFileIndex.ModuleFileNamesList list;

        public ModuleFileNamesListDebugView(OMFFileIndex.ModuleFileNamesList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RawValue<SymString>[] Items => list.ToArray();
    }

    public partial class OMFFileIndex
    {
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ModuleFileNamesListDebugView))]
        public readonly struct ModuleFileNamesList : IEnumerable<RawValue<SymString>>
        {
            private readonly MemoryChunk namesChunk;
            private readonly NativeSpan<int> offsets;
            private readonly bool isLengthPrefixedString;

            public int Count => offsets.Length;

            internal ModuleFileNamesList(MemoryChunk namesChunk, NativeSpan<int> offsets, bool isLengthPrefixedString)
            {
                this.namesChunk = namesChunk;
                this.offsets = offsets;
                this.isLengthPrefixedString = isLengthPrefixedString;
            }

            public RawValue<SymString> this[int index]
            {
                get
                {
                    //NativeSpan will do bounds checking for us
                    var offset = offsets[index];

                    return ReadString(namesChunk, offset, isLengthPrefixedString);
                }
            }

            public Enumerator GetEnumerator() => new Enumerator(namesChunk, offsets, isLengthPrefixedString);

            IEnumerator<RawValue<SymString>> IEnumerable<RawValue<SymString>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal static RawValue<SymString> ReadString(MemoryChunk namesChunk, int offset, bool isLengthPrefixedString)
            {
                var str = namesChunk.PeekSymString(offset);

                return new RawValue<SymString>(namesChunk.AbsoluteOffset + offset, str);
            }

            public struct Enumerator : IEnumerator<RawValue<SymString>>
            {
                private int offsetIndex;
                private readonly MemoryChunk namesChunk;
                private readonly NativeSpan<int> offsets;
                private readonly bool isLengthPrefixedString;

                internal Enumerator(MemoryChunk namesChunk, NativeSpan<int> offsets, bool isLengthPrefixedString)
                {
                    this.namesChunk = namesChunk;
                    this.offsets = offsets;
                    this.isLengthPrefixedString = isLengthPrefixedString;
                    offsetIndex = 0;

                    Current = default;
                }

                public bool MoveNext()
                {
                    if (offsetIndex < offsets.Length)
                    {
                        Current = ModuleFileNamesList.ReadString(namesChunk, offsets[offsetIndex], isLengthPrefixedString);

                        offsetIndex++;

                        return true;
                    }

                    Current = default;
                    return false;
                }

                public RawValue<SymString> Current { get; private set; }

                object IEnumerator.Current => Current;

                public void Reset()
                {
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
