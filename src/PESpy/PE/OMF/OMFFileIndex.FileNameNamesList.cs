using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    internal class FileNameNamesListDebugView
    {
        private OMFFileIndex.FileNameNamesList list;

        public FileNameNamesListDebugView(OMFFileIndex.FileNameNamesList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public OMFFileIndex.ModuleFileNamesList[] Items => list.ToArray();
    }

    internal class FlatEnumeratorDebugView
    {
        private OMFFileIndex.FileNameNamesList.FlatEnumerator enumerator;

        public FlatEnumeratorDebugView(OMFFileIndex.FileNameNamesList.FlatEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public RawValue<FixedUtf8String>[] Items => enumerator.ToArray();
    }

    public partial class OMFFileIndex
    {
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(FileNameNamesListDebugView))]
        public readonly struct FileNameNamesList : IEnumerable<ModuleFileNamesList>
        {
            private readonly MemoryChunk namesChunk;
            private readonly OMFFileIndex info;
            internal readonly bool isLengthPrefixedString;

            public int Count => info.NumModules;

            internal FileNameNamesList(MemoryChunk namesChunk, OMFFileIndex info, bool isLengthPrefixedString)
            {
                this.namesChunk = namesChunk;
                this.info = info;
                this.isLengthPrefixedString = isLengthPrefixedString;
            }

            public ModuleFileNamesList this[int index]
            {
                get
                {
                    //FileNameOffsetsList will do bounds checks for us
                    var offsets = info.FileNameOffsets[index];

                    return new ModuleFileNamesList(namesChunk, offsets, isLengthPrefixedString);
                }
            }

            public FixedUtf8String GetString(int offset) => ModuleFileNamesList.ReadString(namesChunk, offset, isLengthPrefixedString).Value;

            /// <summary>
            /// Provides access to a flat enumeration of all of the files contained in this jagged array.
            /// </summary>
            /// <returns></returns>
            public FlatEnumerator AsFlat() => new FlatEnumerator(namesChunk, info.FileNameOffsets.AsFlat(), isLengthPrefixedString);

            public Enumerator GetEnumerator() => new Enumerator(namesChunk, info, isLengthPrefixedString);

            IEnumerator<ModuleFileNamesList> IEnumerable<ModuleFileNamesList>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<ModuleFileNamesList>
            {
                private int numModules;
                private int moduleIndex;
                private readonly MemoryChunk namesChunk;
                private readonly OMFFileIndex info;
                private readonly bool isLengthPrefixedString;

                internal Enumerator(MemoryChunk namesChunk, OMFFileIndex info, bool isLengthPrefixedString)
                {
                    numModules = info.NumModules;
                    this.namesChunk = namesChunk;
                    this.info = info;
                    moduleIndex = 0;
                    this.isLengthPrefixedString = isLengthPrefixedString;

                    Current = default;
                }

                public bool MoveNext()
                {
                    if (moduleIndex < numModules)
                    {
                        var offsets = info.FileNameOffsets[moduleIndex];

                        Current = new ModuleFileNamesList(namesChunk, offsets, isLengthPrefixedString);

                        moduleIndex++;

                        return true;
                    }

                    Current = default;
                    return false;
                }

                public ModuleFileNamesList Current { get; private set; }

                object IEnumerator.Current => Current;

                public void Reset()
                {
                }

                public void Dispose()
                {
                }
            }

            [DebuggerDisplay("Count = {offsets.Length}")]
            [DebuggerTypeProxy(typeof(FlatEnumeratorDebugView))]
            public struct FlatEnumerator : IEnumerable<RawValue<FixedUtf8String>>, IEnumerator<RawValue<FixedUtf8String>>
            {
                private int offsetIndex;
                private readonly MemoryChunk namesChunk;
                private readonly NativeSpan<int> offsets;
                private readonly bool isLengthPrefixedString;

                public int Count => offsets.Length;

                internal FlatEnumerator(MemoryChunk namesChunk, NativeSpan<int> offsets, bool isLengthPrefixedString)
                {
                    this.namesChunk = namesChunk;
                    this.offsets = offsets;
                    this.isLengthPrefixedString = isLengthPrefixedString;

                    Current = default;
                    offsetIndex = default;
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

                public FlatEnumerator GetEnumerator() => new FlatEnumerator(namesChunk, offsets, isLengthPrefixedString);

                IEnumerator<RawValue<FixedUtf8String>> IEnumerable<RawValue<FixedUtf8String>>.GetEnumerator() => GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
