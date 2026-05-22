#if !DISABLE_REVIEW
using PInvoke;
using ReView;

namespace PESpy.TreeView
{
    internal class TreeViewPanel : UIElement
    {
        public TreeViewPanel(out TreeViewPanel field)
        {
            field = this;
            Dock = DockStyle.Fill;
            Size = new SIZE(301, 778);
        }

        protected override void OnPaint(HDC hdc)
        {
            User32.FillRect(hdc, ClientRectangle, Gdi32.CreateSolidBrush(new COLORREF(0, 0, 255)));
        }
    }
}
#endif
