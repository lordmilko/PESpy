#if !DISABLE_REVIEW
using PInvoke;
using ReView;

namespace PESpy.TextView
{
    internal class TextViewPanel : UIElement
    {
        public TextViewPanel(out TextViewPanel field)
        {
            field = this;
            Dock = DockStyle.Fill;
        }

        protected override void OnPaint(HDC hdc)
        {
            User32.FillRect(hdc, ClientRectangle, Gdi32.CreateSolidBrush(new COLORREF(255, 0, 0)));
        }
    }
}
#endif
