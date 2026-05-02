using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    internal class ViewChildListDebugView
    {
        private ViewChildList list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Items => list.ToArray();

        internal ViewChildListDebugView(ViewChildList list)
        {
            this.list = list;
        }
    }

    //Boxes
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ViewChildListDebugView))]
    public readonly struct ViewChildList : IEnumerable<IView>
    {
        private readonly int parentOffset;
        private readonly IViewable parent;
        private readonly ViewWriter viewWriter;
        private readonly int numChildren;
        private readonly IView _parentView;

        public int Count => numChildren;

        internal ViewChildList(int parentOffset, IViewable parent, ViewWriter viewWriter, IView parentView)
        {
            this.parentOffset = parentOffset;
            this.parent = parent;
            this.viewWriter = viewWriter;
            numChildren = parent.NumChildren();
            _parentView = parentView;
        }

        public IView this[int index] => viewWriter.GetChild(parentOffset, parent, index, _parentView);

        public IView First() => this[0];

        public IView Last() => this[numChildren - 1];

        public Enumerator GetEnumerator() => new Enumerator(parentOffset, parent, viewWriter, numChildren, _parentView);

        IEnumerator<IView> IEnumerable<IView>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<IView>
        {
            private readonly int parentOffset;
            private readonly IViewable parent;
            private readonly ViewWriter viewWriter;
            private readonly int numChildren;
            private readonly IView _parentView;
            private int index;

            internal Enumerator(int parentOffset, in IViewable parent, ViewWriter viewWriter, int numChildren, IView parentView)
            {
                this.parentOffset = parentOffset;
                this.parent = parent;
                this.viewWriter = viewWriter;
                this.numChildren = numChildren;
                _parentView = parentView;
                index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (index < numChildren)
                {
                    Current = viewWriter.GetChild(parentOffset, parent, index, _parentView);
                    Debug.Assert(Current.Parent == _parentView);
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
