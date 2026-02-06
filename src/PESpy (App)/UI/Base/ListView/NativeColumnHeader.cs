using PInvoke;

namespace PESpy.UI
{
    internal class NativeColumnHeader
    {
        public string Text { get; set; }

        public int Index { get; set; }
        public NativeListView ListView { get; set; }

        private int _width;

        public int Width
        {
            get
            {
                if (ListView != null && ListView.IsHandleCreated)
                {
                    var hdr = (HWND) (nint) User32.SendMessageW(ListView.hWnd, (int) LVM.LVM_GETHEADER, default, default);

                    if (hdr != default)
                    {
                        var count = User32.SendMessageW(hdr, (int) HDM.HDM_GETITEMCOUNT, default, default);

                        if (Index < count)
                        {
                            _width = User32.SendMessageW(ListView.hWnd, (int) LVM.LVM_GETCOLUMNWIDTH, Index, default);
                        }
                    }
                }

                return _width;
            }
            set
            {
                _width = value;

                if (ListView == null || !ListView.IsHandleCreated)
                    return;

                User32.SendMessageW(ListView.hWnd, (int) LVM.LVM_SETCOLUMNWIDTH, Index, value);
            }
        }

        public NativeColumnHeader(string text, int width = 0)
        {
            Text = text;
            _width = width;
        }

        internal static unsafe void Realize(
            NativeListView listView,
            in NativeColumnHeader columnHeader,
            int index)
        {
            var column = new LVCOLUMNW
            {
                mask = LVCOLUMNW_MASK.LVCF_TEXT | LVCOLUMNW_MASK.LVCF_WIDTH,
                cx = columnHeader.Width
            };

            fixed (char* c = columnHeader.Text)
            {
                column.pszText = c;

                User32.SendMessageW(listView.hWnd, (int) LVM.LVM_INSERTCOLUMNW, index, (nint) (&column));
            }

            columnHeader.Index = index;
            columnHeader.ListView = listView;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
