using PInvoke;

namespace PESpy.UI
{
    //The way that WinForms works is that in response to WmShowWindow, it calls CreateControl, which causes it
    //to iterate over all children and call CreateControl on them, causing them to create their handle, and any
    //of their children as well
    public class NativeForm : NativeWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var d = base.CreateParams;

                d.Style = (int) (WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_CLIPCHILDREN | WINDOW_STYLE.WS_CLIPSIBLINGS);
                d.ExStyle = WINDOW_EX_STYLE.WS_EX_APPWINDOW | WINDOW_EX_STYLE.WS_EX_WINDOWEDGE;

                var workingArea = GetWorkingArea();

                d.X = (workingArea.Width - d.Width) / 2;
                d.Y = (workingArea.Height - d.Height) / 2;
                d.Caption = Text;

                return d;
            }
        }

        private unsafe static RECT GetWorkingArea()
        {
            var multiMonitorSupport = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CMONITORS) != 0;

            if (multiMonitorSupport)
            {
                //If you just get SM_CXSCREEN and SM_CYSCREEN you won't do the same thing as WinForms; WinForms takes the taskbar into account
                //by instead asking for the "working area" of the screen
                User32.GetCursorPos(out var pt);

                //Don't need to free this
                var hMonitor = User32.MonitorFromPoint(pt, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

                MONITORINFO mi = new MONITORINFO
                {
                    cbSize = sizeof(MONITORINFO)
                };
                User32.GetMonitorInfoW(hMonitor, &mi);

                return mi.rcWork;
            }
            else
            {
                RECT rect;
                User32.SystemParametersInfoW(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, (void*) (&rect), default);

                return rect;
            }
        }

        public NativeForm()
        {
            Size = new SIZE(300, 300);

            SetState(WindowState.TopLevel, true);
            SetState(WindowState.Visible, false); //NativeWindow says that all controls are visible by default, but that's not the case for forms!
        }

        protected override void WmPaint(HDC hdc)
        {
            User32.GetClientRect(hWnd, out var rect);

            var hBrush = DefaultBackgroundBrush;

            User32.FillRect(hdc, rect, hBrush);
        }
    }
}
