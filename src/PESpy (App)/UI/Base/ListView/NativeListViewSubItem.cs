using System.Drawing;
using PInvoke;

namespace PESpy.UI
{
    public class NativeListViewSubItem
    {
        public string Text { get; set; }

        public COLORREF ForeColor { get; set; }

        public COLORREF BackColor { get; set; }

        public object Tag { get; set; }

        //Index of the subitem
        public int Index { get; set; }

        public Rectangle Bounds => _owner.ListView.GetSubItemRect(_owner.Index, Index);

        private NativeListViewItem _owner;

        public NativeListViewSubItem(NativeListViewItem owner, string text)
        {
            _owner = owner;
            Text = text ?? string.Empty;
        }

        internal unsafe static void Realize(NativeListViewSubItem subItem, NativeListView listView, int index)
        {
            fixed (char* c = subItem.Text)
            {
                var item = new LVITEMW
                {
                    mask = LIST_VIEW_ITEM_FLAGS.LVIF_TEXT,
                    iSubItem = subItem.Index,
                    pszText = c
                };

                User32.SendMessageW(listView.hWnd, (int) LVM.LVM_SETITEMTEXTW, index, (nint) (&item));
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
