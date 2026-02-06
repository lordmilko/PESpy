using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using PInvoke;

namespace PESpy.UI
{
    [DebuggerDisplay("Count = {Count}")]
    internal class NativeListViewColumnHeaderCollection : IEnumerable<NativeColumnHeader>
    {
        public int Count { get; private set; }

        private NativeListView _listView;

        private NativeColumnHeader[] _columnHeaders;

        internal NativeListViewColumnHeaderCollection(NativeListView listView)
        {
            _listView = listView;
        }

        public NativeColumnHeader this[int index] => _columnHeaders[index];

        public void Add(string text) => Add(text, 0);

        public void Add(string text, int width)
        {
            //InsertColumn
            //InsertColumnNative
            var header = new NativeColumnHeader(text, width);

            if (_listView.IsHandleCreated)
                NativeColumnHeader.Realize(_listView, header, Count);

            InsertItem(header);
        }

        public void Add(NativeColumnHeader header)
        {
            if (_listView.IsHandleCreated)
                NativeColumnHeader.Realize(_listView, header, Count);

            InsertItem(header);
        }

        public int IndexOfText(string key)
        {
            var columnHeaders = _columnHeaders;

            for (var i = 0; i < Count; i++)
            {
                if (columnHeaders[i].Text == key)
                    return i;
            }

            return -1;
        }

        private void InsertItem(in NativeColumnHeader columnHeader)
        {
            if (_columnHeaders == null)
                _columnHeaders = new NativeColumnHeader[4];
            else
            {
                if (Count >= _columnHeaders.Length)
                    Array.Resize(ref _columnHeaders, Count * 2);
            }

            _columnHeaders[Count] = columnHeader;
            Count++;
        }

        public void Clear()
        {
            if (Count == 0)
                return;

            //WinForms goes last to first, perhaps to avoid having to shuffle each item down as you delete them

            if (_listView.IsHandleCreated)
            {
                for (var i = Count - 1; i >= 0; i--)
                    User32.SendMessageW(_listView.hWnd, (int) LVM.LVM_DELETECOLUMN, i, default);
            }

            Count = 0;
            Array.Clear(_columnHeaders);
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, _columnHeaders);

        IEnumerator<NativeColumnHeader> IEnumerable<NativeColumnHeader>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<NativeColumnHeader>
        {
            private readonly int _limit;
            private int _index;
            private NativeColumnHeader[] _columnHeaders;

            public Enumerator(int count, NativeColumnHeader[] columnHeaders)
            {
                _index = -1;
                _limit = count - 1;
                _columnHeaders = columnHeaders;
            }

            public NativeColumnHeader Current => _columnHeaders[_index];

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
