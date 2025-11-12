using System.ComponentModel;
#if WINFORMS
using System.Windows.Forms;
#endif

namespace PESpy.Controls
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
