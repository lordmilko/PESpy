using System;
using System.Diagnostics;
using System.Drawing;
using PInvoke;

namespace PESpy.UI
{
    public class NativeListViewItem
    {
        //In WinForms, SubItems is a property and when you access the getter, if the SubItemsCount is 0
        //it adds a ListViewItem for the ListViewItem itself. We don't support clearing subitems so we should always
        //have at least 1 item
        public string? Text
        {
            get => SubItems[0].Text;
            set => SubItems[0].Text = value;
        }

        public unsafe int Index
        {
            get
            {
                LVFINDINFOW info = new LVFINDINFOW
                {
                    lParam = (nint) _lParam,
                    flags = LVFINDINFOW_FLAGS.LVFI_PARAM
                };

                var result = User32.SendMessageW(ListView.hWnd, (int) LVM.LVM_FINDITEMW, -1, (nint) (&info));

                Debug.Assert(result != -1);

                return result;
            }
        }

        public COLORREF BackColor { get; }

        public object? Tag { get; set; }
        public Rectangle Bounds
        {
            get
            {
                if (ListView == null)
                    return default;

                return ListView.GetItemRectOrEmpty(Index);
            }
        }

        internal NativeListView ListView { get; set; }
        public NativeListViewItem(string text) : this()
        {
            Text = text;
        }
        public override string ToString()
        {
            return Text;
        }
    }
}
