#if !DISABLE_REVIEW
using PInvoke;
using ReView;

namespace PESpy.NavBar
{
    internal class NavBarPanel : UIElement
    {
        public NavBarPanel(out NavBarPanel field)
        {
            field = this;
            Dock = DockStyle.Top;

            Height = 50;
        }

        protected override void OnPaint(HDC hdc)
        {
            User32.FillRect(hdc, ClientRectangle, Gdi32.CreateSolidBrush(new COLORREF(128, 128, 128)));
        }
    }
}
#endif
