using PInvoke;

namespace PESpy.Controls
{
    //The way that WinForms works is that in response to WmShowWindow, it calls CreateControl, which causes it
    //to iterate over all children and call CreateControl on them, causing them to create their handle, and any
    //of their children as well
    public abstract class NativeForm : NativeWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var d = base.CreateParams;

                d.Style = (int) (WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_CLIPCHILDREN | WINDOW_STYLE.WS_CLIPSIBLINGS);
                d.ExStyle = WINDOW_EX_STYLE.WS_EX_APPWINDOW | WINDOW_EX_STYLE.WS_EX_WINDOWEDGE;

                var screenWidth = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN);
                var screenHeight = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSCREEN);

                d.X = (screenWidth - d.Width) / 2;
                d.Y = (screenHeight - d.Height) / 2;
                d.Caption = Text;

                return d;
            }
        }

        public NativeForm()
        {
            Size = new SIZE(300, 300);
        }

        protected override unsafe void WmCreate(ref Message m, CREATESTRUCTW* pCreateStruct)
        {
        }

        protected override void WmPaint(HDC hdc)
        {
            User32.GetClientRect(hWnd, out var rect);

            var hBrush = DefaultBackgroundBrush;

            User32.FillRect(hdc, rect, hBrush);
        }
    }
}
