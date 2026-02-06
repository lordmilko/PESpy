using PInvoke;

namespace PESpy.UI
{
    internal class LoadingPanel : NativeWindow
    {
        public LoadingPanel(out LoadingPanel field)
        {
            field = this;
        }

        protected override void WmPaint(HDC hdc)
        {
            User32.FillRect(hdc, ClientRectangle, Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.BLACK_BRUSH));

            base.WmPaint(hdc);
        }
    }
}
