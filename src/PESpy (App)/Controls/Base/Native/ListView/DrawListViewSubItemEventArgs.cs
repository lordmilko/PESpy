#if !WINFORMS
using System;
using System.Drawing;

namespace PESpy.Controls
{
    public class DrawListViewSubItemEventArgs
    {
        public int ColumnIndex => throw new NotImplementedException();

        public NativeListViewItem Item => throw new NotImplementedException();

        public NativeListViewItem.NativeListViewSubItem SubItem => throw new NotImplementedException();

        public Rectangle Bounds => throw new NotImplementedException();
    }
}
#endif
