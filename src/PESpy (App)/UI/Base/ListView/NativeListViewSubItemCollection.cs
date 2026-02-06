using System;
using System.Diagnostics;

namespace PESpy.UI
{
    [DebuggerDisplay("Count = {Count}")]
    public struct NativeListViewSubItemCollection
    {
        public int Count { get; private set; }

        private NativeListViewItem _listViewItem;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private NativeListViewSubItem[] _subItems;

        internal NativeListViewSubItemCollection(NativeListViewItem listViewItem)
        {
            _listViewItem = listViewItem;
            Add(new NativeListViewSubItem(listViewItem, string.Empty)); //WinForms sets the text to empty, and then when you set the ListView text it stores it here
        }

        public NativeListViewSubItem Add(NativeListViewSubItem subItem)
        {
            InsertItem(subItem);

            if (_listViewItem.ListView == null || !_listViewItem.ListView.IsHandleCreated)
                return subItem;
        }
        public void AddRange(NativeListViewSubItem[] subItems)
        {
            InsertItems(subItems);

            if (_listViewItem.ListView == null || !_listViewItem.ListView.IsHandleCreated)
                return;
        }
        public NativeListViewSubItem this[int index]
        {
            get
            {
                return _subItems[index];
            }
            set
            {
                //We only support indexing during setup
                _subItems[index] = value;
            }
        }

        private void InsertItem(NativeListViewSubItem subItem)
        {
            if (_subItems == null)
                _subItems = new NativeListViewSubItem[4];
            else
            {
                if (Count >= _subItems.Length)
                    Array.Resize(ref _subItems, Count * 2);
            }

            subItem.Index = Count;

            _subItems[Count] = subItem;
            Count++;
        }

        private void InsertItems(NativeListViewSubItem[] subItems)
        {
            if (_subItems == null)
                _subItems = subItems;
            else
            {
                var requiredLength = Count + subItems.Length;

                if (requiredLength >= _subItems.Length)
                {
                    var newSize = Math.Max(requiredLength, _subItems.Length * 2);
                    Array.Resize(ref _subItems, requiredLength);
                }

                Array.Copy(subItems, 0, _subItems, Count, subItems.Length);
            }

            Count += subItems.Length;
        }
}
