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
        public RawValue<FixedUtf8String>[] Items => list.ToArray();
    }

    public partial class OMFFileIndex
    {
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ModuleFileNamesListDebugView))]
        public readonly struct ModuleFileNamesList : IEnumerable<RawValue<FixedUtf8String>>
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

            public RawValue<FixedUtf8String> this[int index]
            {
                get
                {
                    //NativeSpan will do bounds checking for us
                    var offset = offsets[index];

                    return ReadString(namesChunk, offset, isLengthPrefixedString);
                }
            }

            public Enumerator GetEnumerator() => new Enumerator(namesChunk, offsets, isLengthPrefixedString);

            IEnumerator<RawValue<FixedUtf8String>> IEnumerable<RawValue<FixedUtf8String>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            internal static RawValue<FixedUtf8String> ReadString(MemoryChunk namesChunk, int offset, bool isLengthPrefixedString)
            {
                if (isLengthPrefixedString)
                {
                    var strLen = namesChunk.PeekByte(offset);
                    var str = namesChunk.PeekUtf8FixedLength(offset + 1, strLen);
                    return new RawValue<FixedUtf8String>(namesChunk.AbsoluteOffset + offset, str);
                }
                else
                {
                    var str = namesChunk.PeekAnsiNullTerminatedString(offset);

                    return new RawValue<FixedUtf8String>(namesChunk.AbsoluteOffset + offset, (FixedUtf8String) str);
                }
            }

            public struct Enumerator : IEnumerator<RawValue<FixedUtf8String>>
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

                public RawValue<FixedUtf8String> Current { get; private set; }

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
