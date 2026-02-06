using PInvoke;

namespace PESpy.UI
{
    internal class NativeSplitter : NativeWindow
    {
        private bool _resizing;

        public Orientation Orientation { get; set; } = Orientation.Vertical;

        protected override void WmPaint(HDC hdc)
        {
            User32.FillRect(hdc, ClientRectangle, DefaultBackgroundBrush);
        }
        protected override void WmMouseDown(ref Message m, int x, int y)
        {
            base.WmMouseDown(ref m, x, y);

            SetResizeCursor();

            _resizing = true;
        }

        protected override void WmMouseLeave(ref Message m)
        {
            base.WmMouseLeave(ref m);

            var hCursor = User32.LoadCursorW(default, UxTheme.IDC_ARROW);
            User32.SetCursor(hCursor);
        }

        protected override void WmMouseUp(ref Message m, int x, int y)
        {
            base.WmMouseUp(ref m, x, y);

            _resizing = false;
        }
}
