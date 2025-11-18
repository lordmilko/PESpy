using System;
using PInvoke;

namespace PESpy.Controls
{
    internal class NavComboBox : NativeComboBox
    {
        private NavListBox _listBox;
        internal INavAccessor NavAccessor;
        private HFONT _hFont;

        public string Name { get; }

        internal NavListBox ListBox => _listBox;

        public event EventHandler SelectedIndexChanged;

        unsafe static NavComboBox()
        {
            /* We wish to have a lazy combobox that allows storing a theoretically limited number of items. A combobox displays its items within a ListBox. When adding items to the ComboBox, there are two major points
             * that cause delay: the first, each time an element is added it wants to calculate the new size of the ListBox (you can fix this by setting CBS.CBS_NOINTEGRALHEIGHT). The second, each time an element
             * is inserted it wants to notify the parent.
             * 
             * Rather than set all these items, the best thing to do would ideally be to make the ListBox lazy. You can make a lazy ListBox by setting the LBS_NODATA style when the ListBox
             * is created. The ComboBox does not provide a mechanism to set this style. As such, we'll go with Plan B: hook CreateWindowEx, and when we see
             * we're creating a ComboBox, modify the styles to include LBS_NODATA
             */

            //SetWindowsHookExW didn't seem to work for me, so we'll try an IAT patch instead

            Hooks.InstallCreateWindowExWHook();
        }        

        internal NavComboBox(string name, INavAccessor navAccessor, HFONT hFont)
        {
            Name = name;
            NavAccessor = navAccessor;
            navAccessor.Self = this;
            _hFont = hFont;

            SelectedIndexChanged += (s, e) => navAccessor.SelectedIndexChanged(SelectedIndex);
        }

        internal int SelectedIndex
        {
            get => User32.SendMessageW(hWnd, (int) CB.CB_GETCURSEL, default, default);
            set
            {
                User32.SendMessageW(hWnd, (int) CB.CB_SETCURSEL, value, default);
                RaiseSelectedIndexChanged();
            }
        }

        public unsafe void Refresh(int parentSelectedIndex)
        {
            NavAccessor.NotifyParentChanged(parentSelectedIndex);
            _listBox.Count = NavAccessor.GetCount();
            User32.SendMessageW(hWnd, (int) WM.WM_SETREDRAW, 0, default);

            /* If we set SelectedIndex, it will raise its changed event which will cause
             * the next level to set its index to the default as well. But we're going to
             * call Goto on the next level which will tell it what its correct index is.
             * 
             * We only need to protect against the goto case; in the non-goto case, setting
             * the selected index is essential. But at the same time, we need to raise selected
             * index changed so that the next level also calls refresh (which will in turn call
             * notify parent changed). So our solution for now is just to disable redraw, that way
             * we get no flicker*/
            SelectedIndex = 0;

            User32.SendMessageW(hWnd, (int) WM.WM_SETREDRAW, 1, default);
            User32.InvalidateRect(hWnd, (RECT*) default, false);
        }

        public void RaiseSelectedIndexChanged() => SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

        protected override unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
            base.WmCreate(ref m, pCreateStruct);

            var buff = stackalloc byte[100];

            var comboBoxInfo = new COMBOBOXINFO
            {
                cbSize = sizeof(COMBOBOXINFO)
            };

            User32.SendMessageW(hWnd, (int) CB.CB_GETCOMBOBOXINFO, default, (nint) (&comboBoxInfo));

            User32.GetClientRect(comboBoxInfo.hwndList, out var rc);

            User32.SendMessageW(hWnd, (int) WM.WM_SETFONT, (nint) _hFont, default);

            _listBox = new NavListBox(Name, comboBoxInfo.hwndList, NavAccessor);
            _listBox.Count = NavAccessor.GetCount();
        }

        protected override unsafe void WmDrawItem(ref Message m, nint id, DRAWITEMSTRUCT* drawItemStruct)
        {
            User32.GetClientRect(_listBox.hWnd, out var rect);

            if (drawItemStruct->itemID == -1)
                return;

            var name = NavAccessor.GetName(drawItemStruct->itemID);
            
            fixed (char* c = name)
            {
                var hdc = drawItemStruct->hDC;

                var color = User32.GetSysColor(SYS_COLOR_INDEX.COLOR_HIGHLIGHT);

                COLORREF oldBkColor = default;
                COLORREF oldTxtColor = default;

                if ((drawItemStruct->itemState & ODS_FLAGS.ODS_SELECTED) != 0)
                {
                    var highlight = new COLORREF(User32.GetSysColor(SYS_COLOR_INDEX.COLOR_HIGHLIGHT));

                    FillRectClr(hdc, &drawItemStruct->rcItem, highlight);

                    oldBkColor = Gdi32.SetBkColor(hdc, highlight);
                    oldTxtColor = Gdi32.SetTextColor(hdc, new COLORREF(User32.GetSysColor(SYS_COLOR_INDEX.COLOR_HIGHLIGHTTEXT)));
                }
                else
                {
                    var brush = (HBRUSH) (nint) User32.SendMessageW(hWnd, (int) WM.WM_CTLCOLORLISTBOX, (WPARAM) (nint) hdc, (LPARAM) (nint) _listBox.hWnd);

                    User32.FillRect(hdc, &drawItemStruct->rcItem, brush);
                }

                //ListBox_PrintCallback sets x to 2
                Gdi32.ExtTextOutW(hdc, 2, drawItemStruct->rcItem.top, default, default, c, name.Length, default);

                if ((drawItemStruct->itemState & ODS_FLAGS.ODS_SELECTED) != 0)
                {
                    Gdi32.SetBkColor(hdc, oldBkColor);
                    Gdi32.SetTextColor(hdc, oldTxtColor);
                }
            }
        }

        private unsafe void FillRectClr(HDC hdc, RECT* rect, COLORREF color)
        {
            //This is what ListBox does
            var old = Gdi32.SetBkColor(hdc, new COLORREF(color));
            Gdi32.ExtTextOutW(hdc, 0, 0, ETO_OPTIONS.ETO_OPAQUE, rect, default, 0, default);
            Gdi32.SetBkColor(hdc, old);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
