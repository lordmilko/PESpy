using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy
{
    internal class ImageThunkDataListDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImageThunkData[] Items { get; }

        internal ImageThunkDataListDebugView(ImageThunkDataList list)
        {
            Items = list.ToArray();
        }
    }

    /// <summary>
    /// Provides access to <see cref="ImageThunkData"/> instances without allocating an array.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImageThunkDataListDebugView))]
    public class ImageThunkDataList : IEnumerable<ImageThunkData>, ILightweightList<ImageThunkDataList.Enumerator, ImageThunkData>
    {
        private readonly MemoryChunk chunk;
        private int? _count;
        private bool _isIAT;

        public unsafe int Count
        {
            get
            {
                if (_count == null)
                {
                    //When reading the IAT, we know how many instances we need to read. It's only when reading FirstThunk/OriginalFirstThunk
                    //on import descriptors that we need to compute our bounds, which is until we hit a "null" thunk entry
                    if (chunk.Is32Bit)
                        _count = new Span<int>(chunk.Pointer, chunk.Remaining / 4).IndexOf(0) + 1;
                    else
                        _count = new Span<long>(chunk.Pointer, chunk.Remaining / 8).IndexOf(0) + 1;
                }

                return _count.Value;
            }
        }

        internal ImageThunkDataList(in MemoryChunk chunk, int? count, bool isIAT)
        {
            this.chunk = chunk;
            _count = count;
            _isIAT = isIAT;
        }

        public ImageThunkData this[int index]
        {
            get
            {
                if (index < 0 || index > Count)
                    throw new IndexOutOfRangeException();

                return new ImageThunkData(chunk.Slice(index * chunk.PointerSize), _isIAT);
            }
        }

        public override string ToString()
        {
            return $"Count = {Count}";
        }

        public Enumerator GetEnumerator() => new Enumerator(chunk, Count, _isIAT);

        IEnumerator<ImageThunkData> IEnumerable<ImageThunkData>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<ImageThunkData>
        {
            private readonly MemoryChunk chunk;
            private readonly int _pointerSize;
            private readonly int _count;
            private readonly bool _isIAT;
            private int _index;

            internal Enumerator(in MemoryChunk chunk, int count, bool isIAT)
            {
                this.chunk = chunk;
                _pointerSize = chunk.PointerSize;
                _count = count;
                _isIAT = isIAT;
            }

            public ImageThunkData Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _count)
                {
                    Current = new ImageThunkData(chunk.Slice(_index * _pointerSize), _isIAT);
                    _index++;
                    return true;
                }

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
