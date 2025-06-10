using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PESpy
{
    class NativeSpanDebugView<T> where T : unmanaged
    {
        private readonly NativeSpan<T> span;

        public NativeSpanDebugView(NativeSpan<T> span)
        {
            this.span = span;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => span.ToArray();
    }

    /// <summary>
    /// Represents an a <see cref="Span{T}"/> around native memory, capable of being stored on the heap.
    /// </summary>
    /// <typeparam name="T">The type of element contained in the span.</typeparam>
    [DebuggerTypeProxy(typeof(NativeSpanDebugView<>))]
    public readonly unsafe struct NativeSpan<T> where T : unmanaged
    {
        private readonly T* pointer;
        private readonly int length;

        public NativeSpan(void* pointer, int length)
        {
            this.pointer = (T*) pointer;
            this.length = length;
        }

        public ref T this[int index]
        {
            get
            {
                if (index >= length)
                    throw new IndexOutOfRangeException();

                //From my initial testing it seems like this ref isn't to the current stack frame. Need more testing
                ref T reference = ref *pointer;

                return ref Unsafe.Add(ref reference, (nint)(uint) index);
            }
        }

        public int Length => length;

        public bool IsEmpty => length == 0;

        public static bool operator ==(NativeSpan<T> left, NativeSpan<T> right)
        {
            if (left.length != right.length)
                return false;

            ref T leftRef = ref *left.pointer;
            ref T rightRef = ref *right.pointer;

            return Unsafe.AreSame(ref leftRef, ref rightRef);
        }

        public static bool operator ==(NativeSpan<T> left, T[] right) => ((Span<T>) left) == ((Span<T>) right);

        public static bool operator !=(NativeSpan<T> left, T[] right) => ((Span<T>) left) != ((Span<T>) right);

        public static bool operator ==(T[] left, NativeSpan<T> right) => ((Span<T>) left) == ((Span<T>) right);

        public static bool operator !=(T[] left, NativeSpan<T> right) => ((Span<T>) left) != ((Span<T>) right);

        public static bool operator !=(NativeSpan<T> left, NativeSpan<T> right) => !(left == right);

        public Enumerator GetEnumerator() => new Enumerator(pointer, length);

        public ref T GetPinnableReference() => ref ((Span<T>) this).GetPinnableReference();

        public void CopyTo(Span<T> destination) => ((Span<T>) this).CopyTo(destination);

        public bool TryCopyTo(Span<T> destination) => ((Span<T>) this).TryCopyTo(destination);

        public static implicit operator Span<T>(NativeSpan<T> span) =>
            new Span<T>(span.pointer, span.length);

        public static implicit operator ReadOnlySpan<T>(NativeSpan<T> span) =>
            new ReadOnlySpan<T>(span.pointer, span.length);

        public static implicit operator T*(NativeSpan<T> span) => span.pointer;

        public NativeSpan<T> Slice(int start)
        {
            if ((uint) start > (uint) length)
                throw new ArgumentOutOfRangeException();

            return new NativeSpan<T>((void*) (pointer + start), length - start);
        }

        public NativeSpan<T> Slice(int start, int length)
        {
            if ((uint) start > (uint) this.length || (uint) length > (uint) (this.length - start))
                throw new ArgumentOutOfRangeException();

            return new NativeSpan<T>((void*) (pointer + start), length);
        }

        #region LINQ

        public bool All(Func<T, bool> predicate)
        {
            foreach (var item in this)
            {
                if (!predicate(item))
                    return false;
            }

            return true;
        }

        public bool Contains(T value)
        {
            for (var i = 0; i < Length; i++)
            {
                if (this[i].Equals(value))
                    return true;
            }

            return false;
        }

        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            foreach (var item in this)
                yield return selector(item);
        }

        public NativeSpan<T> Skip(int count)
        {
            throw new NotImplementedException();
        }

        public NativeSpan<T> Take(int count)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        public T[] ToArray() => ((Span<T>) this).ToArray();

        public List<T> ToList()
        {
            var list = new List<T>(Length);

            for (var i = 0; i < Length; i++)
                list.Add(this[i]);

            return list;
        }

        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                return new string((char*) pointer, 0, length);
            }
            return $"NativeSpan<{typeof(T).Name}>[{length}]";
        }

        public struct Enumerator
        {
            private T* ptr;
            private readonly int length;
            private int index;

            internal Enumerator(T* ptr, int length)
            {
                this.ptr = ptr;
                this.length = length;
                index = 0;
                Current = default;
            }

            public T Current { get; private set; }

            public bool MoveNext()
            {
                if (index < length)
                {
                    Current = *ptr;
                    ptr++;
                    index++;
                    return true;
                }
                else
                {
                    Current = default;
                    return false;
                }
            }
        }
    }
}
