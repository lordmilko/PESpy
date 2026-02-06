using System;
using System.Diagnostics;
using PInvoke;

namespace PESpy.UI
{
    
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

        protected unsafe override void WndProc(ref Message m)
        {
            User32.GetClientRect(hWnd, out var rc);

            //NOTE: if Spy++ is open this seems to totally break LB_GETTEXT,
            //the length always returns from the kernel as 4
            switch ((LB) m.Msg)
            {
                case LB.LB_GETTEXTLEN:
                    m.Result = LbGetTextLen((int) m.wParam);
                    return;

                case LB.LB_GETTEXT:
                    m.Result = LbGetText((int) m.wParam, (char*) (nint) m.lParam);
                    return;
            }
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
