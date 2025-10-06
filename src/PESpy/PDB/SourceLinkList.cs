using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy
{
    internal class SourceLinkListDebugView
    {
        private SourceLinkList list;

        internal SourceLinkListDebugView(SourceLinkList list)
        {
            this.list = list;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public FixedUtf8String[] Items => list.ToArray();
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SourceLinkListDebugView))]
    public readonly struct SourceLinkList : IEnumerable<FixedUtf8String>
    {
        //-1 = "sourcelink"
        //0 = none
        //>1 = sourcelink$1 -> sourcelink$count. count stops before the last empty stream
        private readonly int count;
        private readonly PDBFile pdbFile;

        internal SourceLinkList(int count, PDBFile pdbFile)
        {
            this.count = count;
            this.pdbFile = pdbFile;
        }

        public int Count
        {
            get
            {
                return count switch
                {
                    -1 => 1,
                    _ => count
                };
            }
        }

        public FixedUtf8String this[int index]
        {
            get
            {
                switch (count)
                {
                    case 0:
                        throw new IndexOutOfRangeException();

                    case -1:
                    {
                        if (index != 0)
                            throw new IndexOutOfRangeException();

                        var r = pdbFile.TryGetStreamChunk("sourcelink", out var chunk);
                        Debug.Assert(r);
                        return chunk.PeekUtf8FixedLength(0, chunk.Remaining);
                    }

                    default:
                    {
                        if (index < 0 || index >= count)
                            throw new IndexOutOfRangeException();

                        var name = index switch
                        {
                            0 => "sourcelink$1",
                            1 => "sourcelink$2",
                            _ => $"sourcelink${index + 1}"
                        };

                        var r = pdbFile.TryGetStreamChunk(name, out var chunk);
                        Debug.Assert(false);
                        return chunk.PeekUtf8FixedLength(0, chunk.Remaining);
                    }
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(count, pdbFile);

        IEnumerator<FixedUtf8String> IEnumerable<FixedUtf8String>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<FixedUtf8String>
        {
            private readonly int count;
            private int index;
            private readonly PDBFile pdbFile;

            internal Enumerator(int count, PDBFile pdbFile)
            {
                this.count = count;
                this.pdbFile = pdbFile;
                index = 0;

                Current = default;
            }

            public bool MoveNext()
            {
                switch (count)
                {
                    case 0:
                        return false;

                    case -1:
                    {
                        if (index != 0)
                        {
                            Current = default;
                            return false;
                        }

                        var r = pdbFile.TryGetStreamChunk("sourcelink", out var chunk);
                        Debug.Assert(r);
                        Current = chunk.PeekUtf8FixedLength(0, chunk.Remaining);
                        index++;
                        return true;
                    }

                    default:
                    {
                        if (index >= count)
                        {
                            Current = default;
                            return false;
                        }

                        var name = index switch
                        {
                            0 => "sourcelink$1",
                            1 => "sourcelink$2",
                            _ => $"sourcelink${index + 1}"
                        };

                        var r = pdbFile.TryGetStreamChunk(name, out var chunk);
                        Debug.Assert(r);
                        Current = chunk.PeekUtf8FixedLength(0, chunk.Remaining);
                        index++;
                        return true;
                    }
                }
            }

            public FixedUtf8String Current { get; private set; }

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
