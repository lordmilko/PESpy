using System;
using System.Drawing;

namespace PESpy.Controls
{
    public class WinFormsWindowAdapter : NativeWindow
    {
        protected virtual void OnHandleCreated(EventArgs e)
        {
        }

        protected virtual void OnHandleDestroyed(EventArgs e)
        {
        }

#if !WINFORMS
        protected virtual void OnMouseMove(MouseEventArgs e) => throw new NotImplementedException();

        protected virtual void OnMouseLeave(EventArgs e) => throw new NotImplementedException();
#endif

        protected void Invalidate(in Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        protected void SuspendLayout() => throw new NotImplementedException();

        protected void ResumeLayout() => throw new NotImplementedException();

        protected void BeginUpdate() => throw new NotImplementedException();

        protected void EndUpdate() => throw new NotImplementedException();
    }
}
