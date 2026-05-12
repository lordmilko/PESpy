using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PESpy
{
    /// <summary>
    /// Represents a list that is backed by native memory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public unsafe ref struct NativeList<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count { get; private set; }

        //I've compared NativeMemory.Alloc and Marshal.AllocHGlobal; they are identical, except
        //NativeMemory calls malloc while AllocHGlobal calls LocalAlloc. malloc would dispatch to
        //a Win32 API therefore it seems like AllocHGlobal is more efficient
        private T* _ptr;
        private int _capacity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Span<T> Span => new Span<T>(_ptr, Count);

        public NativeList(int capacity)
        {
            _ptr = (T*) Marshal.AllocHGlobal(capacity * sizeof(T));
            _capacity = capacity;
        }

        public T this[int index]
        {
            get => _ptr[index];
            set
            {
                Debug.Assert(index >= 0 && index < Count);

                _ptr[index] = value;
            }
        }

        public ref T ItemRef(int index) => ref _ptr[index];

        public void Add(in T item)
        {
            var count = Count;
            EnsureCapacity(count + 1);
            _ptr[count] = item;
            Count = count + 1;
        }

        public void Clear() => Count = 0;

        private void EnsureCapacity(int requiredCapacity)
        {
            if (_ptr == default)
            {
                var newCapacity = GetNewCapacity(default, requiredCapacity, _capacity);
                _ptr = (T*) Marshal.AllocHGlobal(newCapacity * sizeof(T));
                _capacity = newCapacity;
            }
            else
            {
                var oldCapacity = _capacity;

                if (requiredCapacity < oldCapacity)
                    return;

                var newCapacity = GetNewCapacity(_ptr, requiredCapacity, oldCapacity);

                var count = Count;

                var newPtr = (T*) Marshal.AllocHGlobal(newCapacity * sizeof(T));
                new Span<T>(_ptr, count).CopyTo(new Span<T>(newPtr, count));
                Marshal.FreeHGlobal((IntPtr) _ptr);
                _ptr = newPtr;
                _capacity = newCapacity;
            }
        }

        public T[] ToArray() => Span.ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNewCapacity(T* ptr, int requiredCapacity, int currentCapacity)
        {
            int newCapacity;

            if (ptr == null)
                newCapacity = 16; //defaultCapacity
            else
                newCapacity = currentCapacity * 2;

            if (newCapacity < requiredCapacity)
                newCapacity = requiredCapacity;

            return newCapacity;
        }

        public void Dispose()
        {
            Count = 0;

            var local = _ptr;

            if (local != default)
            {
                Marshal.FreeHGlobal((IntPtr) local);
                this = default;
            }
        }
    }
}
