using System;
#if WINFORMS
using System.Windows.Forms;
#endif
using PESpy.View;
#if !WINFORMS
using TreeNode = PESpy.Controls.NativeTreeNode;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a tree node that provides access to a collection of equivalently typed
    /// elements in a tabular view.
    /// </summary>
    class ListTreeNode : TreeNodeEx
    {
        public override TreeNodeKind Kind => TreeNodeKind.List;

        internal Array? Array;

        internal int Offset;
        internal int Count;

        /// <summary>
        /// Gets the type of struct that this list provides access to.
        /// </summary>
        public ViewKind ViewKind { get; }

        public ListTreeNode(string name, int iconIndex, Array array, ViewKind viewKind, params TreeNode[] children) : base(name, iconIndex, children)
        {
            Array = array;
            ViewKind = viewKind;
        }

        public ListTreeNode(string name, int iconIndex, int offset, int count, ViewKind viewKind, params TreeNode[] children) : base(name, iconIndex, children)
        {
            Offset = offset;
            Count = count;
            ViewKind = viewKind;
        }
    }
}
