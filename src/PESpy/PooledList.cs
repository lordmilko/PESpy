using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal class PooledListDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items { get; }

        public PooledListDebugView(PooledList<T> pooledList)
        {
            //We're having some issues in Merger wherein with over 500,000 items, the debug view
            //is very slow to copy the array in the debugger
            Items = pooledList.GetArrayUnsafe();
        }
    }

    [DebuggerTypeProxy(typeof(PooledListDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    internal ref struct PooledList<T>
    {
        private const int defaultCapacity = 16;

        public int Count { get; private set; }

        public int Capacity => array.Length;

        public Span<T> Span => array.AsSpan(0, Count);

        private T[] array;

        internal PooledList(int capacity)
        {
            array = ArrayPool<T>.Shared.Rent(capacity);
            Count = 0;
        }

        internal PooledList(List<T> items)
        {
            array = ArrayPool<T>.Shared.Rent(items.Count);
            items.CopyTo(array);
            Count = items.Count;
        }

        internal T[] GetArrayUnsafe() => array;

        public T this[int index]
        {
            get => array[index];
            set
            {
                Debug.Assert(index >= 0 && index <= Count);

                array[index] = value;
            }
        }

        public void Add(in T item)
        {
            var count = Count;
            EnsureCapacity(count + 1);

            array[count] = item;
            Count = count + 1;
        }

        public void AddRange(in PooledList<T> items)
        {
            if (items.Count == 0)
                return;

            var count = Count;
            EnsureCapacity(count + items.Count);
            Debug.Assert(array.Length >= items.Count);

            items.CopyTo(array, count);
            Count = count + items.Count;
        }

        public void AddRange(T[] items)
        {
            if (items.Length == 0)
                return;

            var count = Count;
            EnsureCapacity(count + items.Length);

            items.CopyTo(array, count);
            Count = count + items.Length;
        }

        public void AddRange(List<T> items)
        {
            if (items.Count == 0)
                return;

            var count = Count;
            EnsureCapacity(count + items.Count);

            items.CopyTo(array, count);
            Count = count += items.Count;
        }

        public void Insert(int index, in T value)
        {
            var count = Count;

            if (index > count)
                throw new ArgumentOutOfRangeException();

            if (count == array.Length) //Count == Capacity
                EnsureCapacityForInsert(index, 1);
            else if (index < count)
            {
                Array.Copy(array, index, array, index + 1, count - index);
            }

            array[index] = value;
            Count = count + 1;
        }

        public void InsertRange(int index, in PooledList<T> items)
        {
            if (index > array.Length)
                throw new ArgumentOutOfRangeException();

            var count = items.Count;

            if (count > 0)
            {
                if (array.Length - Count < count)
                    EnsureCapacityForInsert(index, count);
                else if (index < Count)
                {
                    Array.Copy(array, index, array, index + count, Count - index);
                }

                items.CopyTo(array, index);

                Count += count;
            }
        }

        public void InsertRange(int index, T[] items)
        {
            if (index > array.Length)
                throw new ArgumentOutOfRangeException();

            var count = items.Length;

            if (count > 0)
            {
                if (array.Length - Count < count)
                    EnsureCapacityForInsert(index, count);
                else if (index < Count)
                {
                    Array.Copy(array, index, array, index + count, Count - index);
                }

                items.CopyTo(array, index);

                Count += count;
            }
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException();

            if (count < 0)
                throw new ArgumentOutOfRangeException   ();

            if (Count - index < count)
                throw new ArgumentOutOfRangeException();

            if (count > 0)
            {
                Count -= count;

                if (index < Count)
                    Array.Copy(array, index + count, array, index, Count - index);
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            if (array == null)
                return; //We never added any items

            Array.Sort(array, 0, Count, Comparer<T>.Create(comparison));
        }

        public void Reverse()
        {
            if (array == null)
                return; //We never added any items

            Array.Reverse(array, 0, Count);
        }

        public void CopyTo(T[] destination, int index)
        {
            if (array == null)
                return; //We never added any items

            Array.Copy(array, 0, destination, index, Count);
        }

        public void Clear() => Count = 0;

        private void EnsureCapacity(int requiredCapacity)
        {
            var localArray = array;

            var arrayPool = ArrayPool<T>.Shared;

            if (localArray == null)
                array = arrayPool.Rent(GetNewCapacity(requiredCapacity));
            else
            {
                var oldLength = localArray.Length;

                if (requiredCapacity < oldLength)
                    return;

                var newCapacity = GetNewCapacity(requiredCapacity);

                var newArray = arrayPool.Rent(newCapacity);
                Array.Copy(localArray, newArray, oldLength);

#if DEBUG
                arrayPool.Return(localArray, clearArray: true);
#else
                arrayPool.Return(localArray);
#endif
                array = newArray;
            }
        }

        private void EnsureCapacityForInsert(int indexToInsert, int insertionCount)
        {
            //In regular EnsureCapacity, we just copy the entire source array over to the destination array.
            //However, when ensuring capacity for insertion, we need to leave a gap where the insert will be occurring

            if (array == null)
            {
                array = ArrayPool<T>.Shared.Rent(GetNewCapacity(insertionCount));
                return;
            }

            var count = Count;

            int requiredCapacity = checked(count + insertionCount);
            int newCapacity = GetNewCapacity(requiredCapacity);

            // Inline and adapt logic from set_Capacity

            T[] newItems = ArrayPool<T>.Shared.Rent(newCapacity);
            if (indexToInsert != 0)
                Array.Copy(array, newItems, length: indexToInsert);

            if (count != indexToInsert)
                Array.Copy(array, indexToInsert, newItems, indexToInsert + insertionCount, count - indexToInsert);

            ArrayPool<T>.Shared.Return(array);
            array = newItems;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNewCapacity(int capacity)
        {
            var a = array;

            int newCapacity;

            if (a == null)
                newCapacity = defaultCapacity;
            else
                newCapacity = a.Length * 2;

            if (newCapacity < capacity)
                newCapacity = capacity;

            return newCapacity;
        }

        internal T[] ExtractAndClear()
        {
            var array = this.array;
            this.array = null;
            Count = 0;
            return array;
        }

        public T[] ToArray()
        {
            //Using spans seems to upset the debugger

            var count = Count;

            if (count == 0)
                return Array.Empty<T>();

            var arr = new T[count];
            Array.Copy(array, arr, count);
            return arr;
        }

        public void Dispose()
        {
            Count = 0;

            var local = array;

            if (local != null)
            {
#if DEBUG
                ArrayPool<T>.Shared.Return(local, clearArray: true);
#else
                ArrayPool<T>.Shared.Return(local);
#endif
                array = null;
            }
        }
    }
}
