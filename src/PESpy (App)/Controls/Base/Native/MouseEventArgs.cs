#if !WINFORMS
using System.Drawing;

namespace PESpy.Controls
{
    public class MouseEventArgs
    {
        public Point Location => throw new System.NotImplementedException();
    }
}
#endif
