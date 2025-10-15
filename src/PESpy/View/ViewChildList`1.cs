using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy.View
{
    //Box free
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct ViewChildList<TParent> : IEnumerable<IView>
        where TParent : IViewable
    {
        private readonly int parentOffset;
        private readonly TParent parent;
        private readonly ViewWriter viewWriter;
        private readonly int numChildren;
        private readonly IView[]? eagerChildren;

        public int Count => numChildren;

        internal ViewChildList(int parentOffset, TParent parent, ViewWriter viewWriter)
        {
            this.parentOffset = parentOffset;
            this.parent = parent;
            this.viewWriter = viewWriter;
            numChildren = parent.NumChildren;
            eagerChildren = default;
        }

        internal ViewChildList(IView[] eagerChildren)
        {
            this.eagerChildren = eagerChildren;
            numChildren = eagerChildren.Length;
            parentOffset = default;
            parent = default;
            viewWriter = default;
        }

        public IView this[int index] => viewWriter.GetChild(parentOffset, parent, index);

        public Enumerator GetEnumerator() => new Enumerator(parentOffset, parent, viewWriter, numChildren, eagerChildren);

        IEnumerator<IView> IEnumerable<IView>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<IView>
        {
            private readonly int parentOffset;
            private readonly TParent parent;
            private readonly ViewWriter viewWriter;
            private readonly int numChildren;
            private readonly IView[]? eagerChildren;
            private int index;

            internal Enumerator(int parentOffset, in TParent parent, ViewWriter viewWriter, int numChildren, IView[]? eagerChildren)
            {
                this.parentOffset = parentOffset;
                this.parent = parent;
                this.viewWriter = viewWriter;
                this.numChildren = numChildren;
                this.eagerChildren = eagerChildren;
                index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index < numChildren)
                {
                    //This is unfortunate, but I can't think of any other way to do this for the ViewChildList<T> case
                    Current = eagerChildren != null ? eagerChildren[index] : viewWriter.GetChild(parentOffset, parent, index);
                    index++;
                    return true;
                }

                Current = default;
                return false;
            }

            public IView Current { get; private set; }

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
