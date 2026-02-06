using System.ComponentModel;

namespace PESpy.UI
{
    class TreeListViewCancelEventArgs : CancelEventArgs
    {
        public TreeViewItem Node { get; }

        public TreeViewAction Action { get; }

        public TreeListViewCancelEventArgs(TreeViewItem node, TreeViewAction action)
        {
            Node = node;
            Action = action;
        }
    }
}
