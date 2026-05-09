using System.Buffers;
using System.Diagnostics;

namespace PESpy
{
    /// <summary>
    /// Represents a non-allocating stack that is backed by a rented array.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public ref struct ValueStack<T>
    {
        public int Count { get; private set; }

        private T[] _array;

        internal ValueStack(int capacity)
        {
            _array = ArrayPool<T>.Shared.Rent(capacity);
            Count = 0;
        }

        public void Push(in T item)
        {
            var count = Count;
            ValueArrayHelpers<T>.EnsureCapacity(count + 1, ref _array);

            _array[count] = item;
            Count++;
        }

        public T Pop()
        {
            var count = Count - 1;
            Count = count;

            var item = _array[count];

            return item;
        }

        public ref T PeekRef() => ref _array[Count - 1];

        public void Clear() => Count = 0;

        public void Dispose()
        {
            Count = 0;

            var local = _array;

            if (local != null)
            {
#if DEBUG
                ArrayPool<T>.Shared.Return(local, clearArray: true);
#else
                ArrayPool<T>.Shared.Return(local);
#endif
                _array = null;
            }
        }
    }
}
