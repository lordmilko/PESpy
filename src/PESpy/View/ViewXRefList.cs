using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    internal class ViewXRefListDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewXRef[] Items { get; }

        internal ViewXRefListDebugView(ViewXRefList list)
        {
            Items = list.ToArray();
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ViewXRefListDebugView))]
    public readonly struct ViewXRefList : IEnumerable<ViewXRef>
    {
        public int Count => _xrefsHandle.Length;

        private readonly SpanAllocatorHandle _xrefsHandle;
        private readonly FileAccessor _fileAccessor;
        private readonly IView _owner;

        public ViewXRefList(IView owner, FileAccessor? fileAccessor)
        {
            if (fileAccessor != null)
                _xrefsHandle = fileAccessor.GetXRefsHandle(owner.Offset);
            else
                _xrefsHandle = default;

            _fileAccessor = fileAccessor;
            _owner = owner;
        }

        public ViewXRef this[int index]
        {
            get
            {
                var xref = _fileAccessor.GetXRef(_xrefsHandle, index);

                //Other is always used regardless of the Kind
                var other = _fileAccessor.GetView(xref.Other);

                return new ViewXRef(_owner, other, xref.Kind);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(_fileAccessor, _xrefsHandle, _owner);

        IEnumerator<ViewXRef> IEnumerable<ViewXRef>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<ViewXRef>
        {
            private readonly FileAccessor? _fileAccessor;
            private readonly int _endIndex;
            private readonly IView _owner;
            private XRef[]? _xrefsBuffer;
            private int _index;

            internal Enumerator(FileAccessor fileAccessor, SpanAllocatorHandle xrefsHandle, IView owner)
            {
                _fileAccessor = fileAccessor;
                _xrefsBuffer = fileAccessor?.GetXRefBuffer(); //If we weren't given the FileAccessor, the SpanAllocatorHandle should be empty and we won't yield anything
                _index = xrefsHandle.Index;
                _endIndex = _index + xrefsHandle.Length;
                _owner = owner;
            }

            public ViewXRef Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_index < _endIndex)
                {
                    var item = _xrefsBuffer[_index];

                    //Other is always used regardless of the Kind
                    var other = _fileAccessor.GetView(item.Other);

                    Current = new ViewXRef(_owner, other, item.Kind);

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
