using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    internal class TypOrEnumTypeListDebugView<T> where T : unmanaged
    {
        private TypOrEnumTypeList<T> items;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TypOrEnumType[] Items => items.ToArray();

        public TypOrEnumTypeListDebugView(TypOrEnumTypeList<T> items)
        {
            this.items = items;
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TypOrEnumTypeListDebugView<>))]
    public readonly struct TypOrEnumTypeList<T> : IEnumerable<TypOrEnumType> where T : unmanaged
    {
        private readonly NativeSpan<T> items;

        public int Count => items.Length;

        internal TypOrEnumTypeList(NativeSpan<T> items)
        {
            this.items = items;
        }

        public unsafe TypOrEnumType this[int index]
        {
            get
            {
                if (typeof(T) == typeof(CV_ItemId))
                    return new TypOrEnumType((byte*) (T*) items, Unsafe.As<T, CV_ItemId>(ref items[index]));
                else if (typeof(T) == typeof(CV_typ16_t))
                    return new TypOrEnumType((byte*) (T*) items, Unsafe.As<T, CV_typ16_t>(ref items[index]));
                else
                    return new TypOrEnumType((byte*) (T*) items, Unsafe.As<T, CV_typ_t>(ref items[index]));
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(items);

        IEnumerator<TypOrEnumType> IEnumerable<TypOrEnumType>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public unsafe struct Enumerator : IEnumerator<TypOrEnumType>
        {
            private readonly NativeSpan<T> items;
            private int index;

            internal Enumerator(NativeSpan<T> items)
            {
                this.items = items;
                index = 0;

                Current = default;
            }

            public bool MoveNext()
            {
                if (index < items.Length)
                {
                    if (typeof(T) == typeof(CV_ItemId))
                        Current = new TypOrEnumType((byte*) (T*) items, Unsafe.As<T, CV_ItemId>(ref items[index]));
                    else if (typeof(T) == typeof(CV_typ16_t))
                        Current = new TypOrEnumType((byte*) (T*) items, Unsafe.As<T, CV_typ16_t>(ref items[index]));
                    else
                        Current = new TypOrEnumType((byte*) (T*) items, Unsafe.As<T, CV_typ_t>(ref items[index]));

                    index++;

                    return true;
                }

                Current = default;
                return false;
            }

            public TypOrEnumType Current { get; private set; }

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
