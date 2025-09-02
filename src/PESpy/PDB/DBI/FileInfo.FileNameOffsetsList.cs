using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.PDB
{
    internal class FileNameOffsetsListDebugView
    {
        private FileInfo.FileNameOffsetsList list;

        public FileNameOffsetsListDebugView(FileInfo.FileNameOffsetsList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public NativeSpan<int>[] Items => list.ToArray();
    }

    public partial class FileInfo
    {
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(FileNameOffsetsListDebugView))]
        public readonly struct FileNameOffsetsList : IEnumerable<NativeSpan<int>>
        {
            private readonly FileInfo info;
            private readonly int baseOffset;

            internal FileNameOffsetsList(FileInfo info)
            {
                this.info = info;

                //Skip over NumModules (2, NumSourceFiles (2), ModuleIndices (Count * 2) and ModuleFileCounts (Count * 2)
                baseOffset = 4 + (Count * 4);
            }

            public int Count => info.NumModules;

            public NativeSpan<int> this[int index]
            {
                get
                {
                    if (index < 0 || index >= Count)
                        throw new IndexOutOfRangeException();

                    var numFiles = info.ModuleFileCounts[index];
                    var startIndex = info.ModuleIndices[index];

                    return info.chunk.PeekNativeSpan<int>(baseOffset + (startIndex * sizeof(int)), numFiles);
                }
            }

            public Enumerator GetEnumerator() => new Enumerator(info, baseOffset);

            IEnumerator<NativeSpan<int>> IEnumerable<NativeSpan<int>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<NativeSpan<int>>
            {
                private int numModules;
                private int moduleIndex;
                private NativeSpan<ushort> moduleFileCounts;
                private NativeSpan<ushort> moduleIndices;
                private MemoryChunk chunk;
                private readonly int baseOffset;

                public Enumerator(FileInfo info, int baseOffset)
                {
                    numModules = info.NumModules;
                    moduleFileCounts = info.ModuleFileCounts;
                    moduleIndices = info.ModuleIndices;
                    moduleIndex = 0;
                    chunk = info.chunk;
                    this.baseOffset = baseOffset;
                }

                public bool MoveNext()
                {
                    if (moduleIndex < numModules)
                    {
                        var numItems = moduleFileCounts[moduleIndex];
                        var startIndex = moduleIndices[moduleIndex];

                        Current = chunk.PeekNativeSpan<int>(baseOffset + (startIndex * sizeof(int)), numItems);

                        moduleIndex++;

                        return true;
                    }

                    Current = default;
                    return false;
                }

                public NativeSpan<int> Current { get; private set; }

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
