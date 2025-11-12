using System;
using System.Collections.Generic;

namespace PESpy.Controls
{
    public class NativeListView : SystemWindow
    {
        //Unsorted, also shouldn't be using List<T>

        public bool FullRowSelect => true;

        public bool Focused => throw new NotImplementedException();

        public bool HideSelection => throw new NotImplementedException();

        public NativeColumnHeaderCollection Columns => throw new NotImplementedException();

        public NativeFont Font => throw new System.NotImplementedException();

        public NativeImageList SmallImageList
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public List<NativeListViewItem> Items => throw new System.NotImplementedException();

        public List<NativeListViewGroup> Groups => throw new System.NotImplementedException();

#if !WINFORMS
        protected unsafe virtual void OnDrawSubItem(DrawListViewSubItemEventArgs e) => throw new System.NotImplementedException();
#endif
    }
}
