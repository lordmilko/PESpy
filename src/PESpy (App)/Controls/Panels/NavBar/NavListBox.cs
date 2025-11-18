using System;
using System.Diagnostics;
using PInvoke;

namespace PESpy.Controls
{
    internal class NavListBox : SystemWindow
    {
        internal int Count
        {
            get => User32.SendMessageW(hWnd, (int) LB.LB_GETCOUNT, default, default);
            set
            {
                User32.SendMessageW(hWnd, (int) LB.LB_SETCOUNT, value, default);

                /* ComboBox is not designed to be used lazily. And yet, for the most part it seems to work.
                 * However, I observed a strange issue wherein, when I had two lazy NavComboBox items (one for
                 * sections, one for global data), the section one would work fine, but when I went to expand
                 * the global data one, only 2 pixels should show.
                 * 
                 * Further investigation showed there was an issue with the height of the ListBox. Doing GetClientRect
                 * showed that the height was 0. Theoretically, what's supposed to happen is when you set the bounds
                 * of your ComboBox using SetWindowPos, ComboBox_Position calls ComboBox_CalcControlRects + ComboBox_SetDroppedSize,
                 * which in turn call MoveWindow, which results in your ListBox receiving its height, even when it's not actually
                 * expanded yet.
                 * 
                 * However, what actually happens is extremely puzzling: the height that is passed to MoveWindow is 2!
                 * The ListBox will then receive a series of messages, culimating in WM_WINDOWPOSCHANGING, confirming that
                 * the height we're setting is in fact 2. This is where things get very odd
                 * - When things are working properly, the kernel will mysteriously then send a series of additional messages,
                 *   starting with WM_NCCALCSIZE, ultimately resulting in the height of the window being set
                 * - When things are not working properly, you won't get any more messages after your WM_WINDOWPOSCHANGING event,
                 *   and your window is stuck at 2 pixels large
                 *
                 * I haven't been able to figure out why the kernel decides to send these subsequent messages beginning with WM_NCCALCSIZE
                 * in some circumstances but not others, however a workaround seems to be to just ensure that the ListBox is set to some
                 * random size manually; perhaps that will be enough to tell the kernel "hey, there's a difference now, and we need to recalc
                 * the size" or something
                 */

                User32.SetWindowPos(hWnd, default, 0, 0, 100, 100, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

#if DEBUG
                User32.GetWindowRect(hWnd, out var rect);
                Debug.Assert(rect.Height > 10);
#endif
            }
        }

        public string Name { get; }

        private INavAccessor _navAccessor;

        //Subclass a ListBox
        internal NavListBox(string name, HWND hWnd, INavAccessor navAccessor)
        {
            Name = name;
            _navAccessor = navAccessor;

            var defWndProc = User32.GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);

            InstallWndProc(hWnd, defWndProc);
        }

        internal override unsafe LRESULT WndProc(HWND hWnd, int uMsg, WPARAM wParam, LPARAM lParam)
        {
            User32.GetClientRect(hWnd, out var rc);

            var wm = (WM) uMsg;

            //NOTE: if Spy++ is open this seems to totally break LB_GETTEXT,
            //the length always returns from the kernel as 4
            switch ((LB) uMsg)
            {
                case LB.LB_GETTEXTLEN:
                    return LbGetTextLen((int) wParam);

                case LB.LB_GETTEXT:
                    return LbGetText((int) wParam, (char*) (nint) lParam);
            }
        private LRESULT LbGetTextLen(int index) => _navAccessor.GetName(index).Length;

        private unsafe LRESULT LbGetText(int index, char* pBuffer)
        {
            var name = _navAccessor.GetName(index);

            name.CopyTo(new Span<char>(pBuffer, name.Length));

            return name.Length;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
