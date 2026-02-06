using System;
using System.Collections;
using System.Collections.Generic;
using PInvoke;

namespace PESpy.UI
{
    internal class NativeListViewItemCollection : IEnumerable<NativeListViewItem>
    {
        public int Count => _lParamToItemMap.Count;

        //In order to handle inserts in the middle, we don't track the actual physical index of each item; we store the lParam
        //of each item, and then when we need to get an item we ask the ListView to tell us the index of the item that contains
        //the secret lParam
        private Dictionary<uint, NativeListViewItem> _lParamToItemMap;
        private List<NativeListViewItem> _pendingItems;

        private NativeListView _listView;

        internal NativeListViewItemCollection(NativeListView listView)
        {
            _listView = listView;
            _lParamToItemMap = new Dictionary<uint, NativeListViewItem>();
        }

        public unsafe NativeListViewItem this[int index]
        {
            get
            {
                var lvItem = new LVITEMW
                {
                    //We want the lParam field to be populated; it's not relevant to the actual lookup that we're doing though
                    mask = LIST_VIEW_ITEM_FLAGS.LVIF_PARAM,
                    iItem = index
                };

                User32.SendMessageW(_listView.hWnd, (int) LVM.LVM_GETITEMW, default, (nint) (&lvItem));

                return _lParamToItemMap[lvItem.lParam];
            }
        }

        public void Add(string text) => Add(new NativeListViewItem(text));

        public void Add(NativeListViewItem item) => Insert(Count, item);

        public void AddRange(NativeListViewItem[] items)
        {
            var count = Count;

            if (_listView.IsHandleCreated)
            {
                User32.SendMessageW(_listView.hWnd, (int) LVM.LVM_SETITEMCOUNT, Count + items.Length, default);

                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    RealizeItem(item, count + i);
                }
            }

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                item.ListView = _listView;
            }
        }

        public void Insert(int index, NativeListViewItem item)
        {
            /* There's a bit of an issue when it comes to inserting items: the caller might want the item to be inserted
             * at a specific index. If the ListView handle has been created, we can go ahead and do this right away.
             * However, if it hasn't been created yet, we've now got a bit of a problem: we need to store where we _want_
             * to insert this item later, and insert the item at the desired position when the handle is actually created.
             * 
             * THe way that WinForms handles this is that they ahve a separate array capable of storing the listview items
             * that is used when the handle hasn't been created yet, and then when it is created, that list is purged and
             * the items are written into the listview in the order they've been defined. Currently, I believe our handle
             * has always been created when we start inserting items. However, if that ever turns out to not be the case,
             * we need to be able to cater for that as well */

            if (_listView.IsHandleCreated)
            {
                User32.SendMessageW(_listView.hWnd, (int) LVM.LVM_SETITEMCOUNT, Count + 1, default);

                RealizeItem(item, index);
            }
            else
            {
                if (_pendingItems == null)
                    _pendingItems = new List<NativeListViewItem>();

                _pendingItems.Insert(index, item);
            }
        }

        internal void AddPendingItems()
        {
            var pendingItems = _pendingItems;

            if (pendingItems == null)
                return;

            AddRange(pendingItems.ToArray());

            _pendingItems = null;
        }

        private void StoreItem(NativeListViewItem item)
        {
            /* There's a bit of a problem with inserting listview items: what happens if you create a bunch of items, and then
             * later on decide to insert a new item in the middle of your existing items. If we record the index of each listviewm
             * item manually on each object, we'll need to increment that ID by 1 every time we insert new records in the middle.
             * 
             * The way that WinForms solves this is they say, rather than store the index, we'll instead store a unique "identifier"
             * per listview item, which we'll stash in its lParam field. Then, in the event we're ever asked to return the index of
             * a given listview item, we'll instead instruct the native listview to lookup the entry whose lParam corresponds to the ID
             * stashed on our managed listview item. This will then give us the "current" native ID, without us needing to worry about
             * updating IDs manually. This is how the WinForms ListView manages things; it refers to the "true" native index as the "display index"
             */

            item._lParam = _listView.GenerateUniqueId();

            item.ListView = _listView;

            _lParamToItemMap[item._lParam] = item;
        }
        private unsafe void RealizeItem(NativeListViewItem listViewItem, int index)
        {
            StoreItem(listViewItem);

            fixed (char* c = listViewItem.Text)
            {
                var item = new LVITEMW
                {
                    mask = LIST_VIEW_ITEM_FLAGS.LVIF_TEXT | LIST_VIEW_ITEM_FLAGS.LVIF_PARAM,
                    iItem = index,
                    pszText = c,

                    
                    lParam = (LPARAM) (nint) listViewItem._lParam
                };

                listViewItem._lParam = item.lParam;
                _lParamToItemMap[item.lParam] = listViewItem;

                //Unlike with LVM_INSERTCOLUMN where the wParam was the index, here the wParam is unused
                User32.SendMessageW(_listView.hWnd, (int) LVM.LVM_INSERTITEMW, default, (nint) (&item));
            }

            for (var i = 1; i < listViewItem.SubItems.Count; i++)
                NativeListViewSubItem.Realize(listViewItem.SubItems[i], _listView, index);
        }

        public void Clear()
        {
            if (Count == 0)
                return;

            User32.SendMessageW(_listView.hWnd, (int) LVM.LVM_DELETEALLITEMS, default, default);
            _lParamToItemMap.Clear();
        }

        public Enumerator GetEnumerator() => new Enumerator(Count, this);

        IEnumerator<NativeListViewItem> IEnumerable<NativeListViewItem>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<NativeListViewItem>
        {
            private readonly int _limit;
            private int _index;
            private NativeListViewItemCollection _items;

            public Enumerator(int count, NativeListViewItemCollection items)
            {
                _index = -1;
                _limit = count - 1;
                _items = items;
            }

            public NativeListViewItem Current => _items[_index];

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
