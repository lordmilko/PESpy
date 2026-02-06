using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy.UI
{
    [DebuggerDisplay("Count = {Count}")]
    public class NativeTreeNodeCollection : IEnumerable<NativeTreeNode>
    {
        public int Count => _children == null ? 0 : _children.Length;

        private NativeTreeNode _node;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private NativeTreeNode[] _children;

        internal NativeTreeNodeCollection(NativeTreeNode node)
        {
            _node = node;
        }

        public NativeTreeNode this[int index] =>
            _children[index];

        public void Add(NativeTreeNode node)
        {
            //Currently only support adding children on load
            Debug.Assert(_children == null);

            node._parent = _node;
            _children = new[] { node };

            node.Realize(false);
        }

        public void AddRange(NativeTreeNode[] nodes)
        {
            //Currently only support adding children on load
            Debug.Assert(_children == null);

            _children = nodes;

            for (var i = nodes.Length - 1; i >= 0; i--)
            {
                var child = nodes[i];
                child._parent = _node;
                child.Realize(false);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, _children);

        IEnumerator<NativeTreeNode> IEnumerable<NativeTreeNode>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<NativeTreeNode>
        {
            private readonly int _limit;
            private int _index;
            private NativeTreeNode[] _columnHeaders;

            public Enumerator(int count, NativeTreeNode[] columnHeaders)
            {
                _index = -1;
                _limit = count - 1;
                _columnHeaders = columnHeaders;
            }

            public NativeTreeNode Current => _columnHeaders[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _limit)
                {
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
