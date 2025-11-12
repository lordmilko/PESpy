using System;
using System.Drawing;

namespace PESpy.Controls
{
    public class NativeListViewItem
    {
        public NativeListViewSubItemCollection SubItems => throw new NotImplementedException();

        public NativeListViewItem() => throw new NotImplementedException();

        public NativeListViewItem(string text) => throw new NotImplementedException();

        public Color ForeColor => throw new NotImplementedException();

        public NativeListViewGroup Group
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public object Tag
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public class NativeListViewSubItem
        {
            public string Name => throw new NotImplementedException();

            public string Text => throw new NotImplementedException();

            public Color ForeColor => throw new NotImplementedException();

            public Color BackColor => throw new NotImplementedException();

            public Rectangle Bounds => throw new NotImplementedException();

            public bool Selected => throw new NotImplementedException();

            public NativeListViewSubItem(NativeListViewItem owner, string text)
            {
                throw new NotImplementedException();
            }

            public object Tag
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
        }
    }
}
