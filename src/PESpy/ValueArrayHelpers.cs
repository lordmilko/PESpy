using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace PESpy
{
    //In a separate class so that ValueStack<T> can share these
    //without needing to create an instantiation of PooledList<T> for
    //each T in ValueStack<T> in NativeAOT
    internal static class ValueArrayHelpers<T>
    {
        private const int defaultCapacity = 16;

        internal static void EnsureCapacity(
            int requiredCapacity,
            ref T[] array)
        {
            var arrayPool = ArrayPool<T>.Shared;

            if (array == null)
                array = arrayPool.Rent(ValueArrayHelpers<T>.GetNewCapacity(requiredCapacity, array));
            else
            {
                var oldLength = array.Length;

                if (requiredCapacity < oldLength)
                    return;

                var newCapacity = ValueArrayHelpers<T>.GetNewCapacity(requiredCapacity, array);

                var newArray = arrayPool.Rent(newCapacity);
                Array.Copy(array, newArray, oldLength);

#if DEBUG
                arrayPool.Return(array, clearArray: true);
#else
                arrayPool.Return(array);
#endif
                array = newArray;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetNewCapacity(int capacity, T[] array)
        {
            int newCapacity;

            if (array == null)
                newCapacity = defaultCapacity;
            else
                newCapacity = array.Length * 2;

            if (newCapacity < capacity)
                newCapacity = capacity;

            return newCapacity;
        }
    }
}
