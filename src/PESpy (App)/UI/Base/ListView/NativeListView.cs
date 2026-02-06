using System;
using System.Linq;
using PInvoke;

namespace PESpy.UI
{
    internal class NativeListView : SystemWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ClassName = "SysListView32";

                createParams.Style |= (int) LVS.LVS_REPORT;

                return createParams;
            }
        }

        private NativeListViewColumnHeaderCollection _columns;
        private NativeListViewItemCollection _items;
        private NativeSelectedListViewItemCollection _selectedItems;

        public NativeListViewColumnHeaderCollection Columns => _columns ??= new NativeListViewColumnHeaderCollection(this);
        public NativeListViewItemCollection Items => _items ??= new NativeListViewItemCollection(this);
        public NativeSelectedListViewItemCollection SelectedItems => _selectedItems ??= new NativeSelectedListViewItemCollection(this);

        public NativeImageList ImageList { get; set; }

        private uint _nextId;

        public NativeListView()
        {
            Border = true;
        }

        internal uint GenerateUniqueId()
        {
            return ++_nextId;
        }

        protected override void OnHandleCreated()
        {
            var exStyles = LVS_EX.LVS_EX_FULLROWSELECT | LVS_EX.LVS_EX_DOUBLEBUFFER | LVS_EX.LVS_EX_GRIDLINES;

            User32.SendMessageW(hWnd, (int) LVM.LVM_SETEXTENDEDLISTVIEWSTYLE, (int) exStyles, (int) exStyles);

            //WinForms applies this hack to fix the ListView not invalidating itself properly. My observation is
            //by default, when you resize columns, the pixels from where the column was before you resized it are
            //not invalidated. This is fixed by either enabling double buffering or setting the color to CLR_NONE.
            //We apply both fixes
            User32.SendMessageW(hWnd, (int) LVM.LVM_SETTEXTBKCOLOR, 0, (int) CLR.CLR_NONE);

            var columns = _columns;

            if (columns?.Count > 0)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    NativeColumnHeader.Realize(this, columns[i], i);
                }
            }

            _items?.AddPendingItems();
        }

        protected unsafe NativeListViewHitTestInfo HitTest(int x, int y)
        {
            if (!ClientRectangle.Contains(x, y))
                return default;

            LVHITTESTINFO lvhi = new LVHITTESTINFO
            {
                pt = new POINT(x, y)
            };

            var itemIndex = User32.SendMessageW(hWnd, (int) LVM.LVM_SUBITEMHITTEST, default, (nint) (&lvhi));

            if (itemIndex == -1)
                return default;

            var item = Items[itemIndex];

            if (lvhi.iSubItem < item.SubItems.Count)
            {
                var subItem = item.SubItems[lvhi.iSubItem];

                return new NativeListViewHitTestInfo(item, subItem);
            }

            return new NativeListViewHitTestInfo(item, null);
        }

        internal unsafe RECT GetItemRectOrEmpty(int index)
        {
            if (index < 0 || index >= Items.Count)
                return default;

            RECT rect = default;

            if (User32.SendMessageW(hWnd, (int) LVM.LVM_GETITEMRECT, index, (IntPtr) (&rect)) == 0)
                return default;

            return rect;
        }

        internal unsafe RECT GetSubItemRect(int itemIndex, int subItemIndex)
        {
            //These are magic numbers https://learn.microsoft.com/en-us/windows/win32/controls/lvm-getsubitemrect
            var itemRect = new RECT
            {
                left = 0, //LVIR_BOUNDS
                top = subItemIndex
            };

            User32.SendMessageW(hWnd, (int) LVM.LVM_GETSUBITEMRECT, itemIndex, (IntPtr) (&itemRect));

            return itemRect;
        }

        protected override void WmMouseHover(ref Message m)
        {
            //When I apply the fullrowselect style, for some reason when I hover over the first item it automatically
            //selects it. Based on WinForms' implementation, this is managed by HoverSelection -> LVS_EX_TRACKSELECT. But
            //I didn't specify that, and even tried explicitly unspecifying it. I do see however that the way WinForms
            //handles WM_MOUSEHOVER is that if the user wants HoverSelection, call the base WndProc. Else call OnMouseHover.
            //This indirectly suppresses passing the event to the listview, which kind of fixes the problem. I don't know if
            //it's possible to fix it properly so this doesn't happen
        }

        public void Clear()
        {
            Items.Clear();
            Columns.Clear();

            //WinForms doesn't do this (perhaps because listview items can be reused?), but when we clear the listview
            //we know none of the items are going to be reused, so I'm going to reset this
            _nextId = 0;
        }
    }
}
