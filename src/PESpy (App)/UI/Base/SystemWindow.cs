namespace PESpy.UI
{
    public abstract class SystemWindow : NativeWindow
    {
        protected override void WmPaint(ref Message m) => DefWndProc(ref m);
    }
}
