using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    /// <summary>
    /// Represents a non-allocating list that is backed by a rented array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count = {Count}")]
    internal ref struct PooledList<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Capacity => array.Length;

        /* There seems to be some sort of fundamental difference between our custom list type and Span
         * that causes the Visual Studio debugger to throw an OutOfMemoryException when we have a list with 10 million
         * integers, but work just fine for Span<T>. Having a DebuggerTypeProxy that returns either a T[] or a Span<T>
         * does not work; the only thing that seems to work for us is bypassing the type proxy entirely and relying on
         * an existing property like this. I haven't figured out why this is yet, but this finally allows us to have
         * a working debugger view of the actual items in the list */
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Span<T> Span => array.AsSpan(0, Count);

        private T[] array;

        internal PooledList(int capacity)
        {
            array = ArrayPool<T>.Shared.Rent(capacity);
            Count = 0;
        }

        internal PooledList(T[] items)
        {
            array = items;
            Count = items.Length;
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

        public ref T ItemRef(int index) => ref array[index];

        public void Add(in T item)
        {
            var count = Count;
            ValueArrayHelpers<T>.EnsureCapacity(count + 1, ref array);

            array[count] = item;
            Count = count + 1;
        }

        public void AddRange(in PooledList<T> items)
        {
            if (items.Count == 0)
                return;

            var count = Count;
            ValueArrayHelpers<T>.EnsureCapacity(count + items.Count, ref array);
            Debug.Assert(array.Length >= items.Count);

            items.CopyTo(array, count);
            Count = count + items.Count;
        }

        public void AddRange(T[] items, int index, int count)
        {
            if (index + count > items.Length)
                throw new ArgumentOutOfRangeException();

            var currentCount = Count;
            ValueArrayHelpers<T>.EnsureCapacity(currentCount + count, ref array);

            Array.Copy(items, index, array, currentCount, count);
            Count = currentCount + count;
        }

        public void AddRange(T[] items)
        {
            if (items.Length == 0)
                return;

            var count = Count;
            ValueArrayHelpers<T>.EnsureCapacity(count + items.Length, ref array);

            items.CopyTo(array, count);
            Count = count + items.Length;
        }

        public void AddRange(List<T> items)
        {
            if (items.Count == 0)
                return;

            var count = Count;
            ValueArrayHelpers<T>.EnsureCapacity(count + items.Count, ref array);

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

        public void Sort(IComparer<T> comparer)
        {
            if (array == null)
                return;

            Array.Sort(array, 0, Count, comparer);
        }

        //We're the key
        public void SortKeyed<TItem>(ref PooledList<TItem> items, IComparer<T>? comparer = null)
        {
            Debug.Assert(Count == items.Count);

            if (array == null)
                return;

            Array.Sort(array, items.array, 0, Count, comparer);
        }

        public IEnumerable<T> Enumerate()
        {
            var array = this.array;
            var count = Count;

            //We can't enumerate inside ourselves directly because that will capture "this"
            //which won't work because we're a ref struct

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerable<T> EnumerateInternal(T[] array, int count)
            {
                for (var i = 0; i < count; i++)
                    yield return array[i];
            }

            return EnumerateInternal(array, count);
        }

        public void Reserve(int amount)
        {
            ValueArrayHelpers<T>.EnsureCapacity(Count + amount, ref array);
            Count += amount;
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

        private void EnsureCapacityForInsert(int indexToInsert, int insertionCount)
        {
            //In regular EnsureCapacity, we just copy the entire source array over to the destination array.
            //However, when ensuring capacity for insertion, we need to leave a gap where the insert will be occurring

            if (array == null)
            {
                array = ArrayPool<T>.Shared.Rent(ValueArrayHelpers<T>.GetNewCapacity(insertionCount, array));
                return;
            }

            var count = Count;

            int requiredCapacity = checked(count + insertionCount);
            int newCapacity = ValueArrayHelpers<T>.GetNewCapacity(requiredCapacity, array);

            // Inline and adapt logic from set_Capacity

            T[] newItems = ArrayPool<T>.Shared.Rent(newCapacity);
            if (indexToInsert != 0)
                Array.Copy(array, newItems, length: indexToInsert);

            if (count != indexToInsert)
                Array.Copy(array, indexToInsert, newItems, indexToInsert + insertionCount, count - indexToInsert);

            ArrayPool<T>.Shared.Return(array);
            array = newItems;
        }

        internal Span<T> GetBuffer(int numItems)
        {
            var newCount = Count + numItems;

            ValueArrayHelpers<T>.EnsureCapacity(newCount, ref array);

            var result = array.AsSpan(Count, numItems);

            Count = newCount;

            return result;
        }

        public bool Contains(T item)
        {
            if (array == null)
                return false; //No items yet

            return Array.IndexOf(array, item, 0, Count) != -1;
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

        public T[] ToArrayAndClear()
        {
            var result = ToArray();
            Clear();
            return result;
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
