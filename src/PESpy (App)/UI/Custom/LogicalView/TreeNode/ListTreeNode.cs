using System;
using PESpy.View;

namespace PESpy.UI
{
    /// <summary>
    /// Represents a tree node that provides access to a collection of equivalently typed
    /// elements in a tabular view.
    /// </summary>
    class ListTreeNode : LogicalTreeNode
    {
        public override LogicalTreeNodeKind Kind => LogicalTreeNodeKind.List;

        internal Array? Array;

        internal int Offset;
        internal int Count;

        /// <summary>
        /// Gets the type of struct that this list provides access to.
        /// </summary>
        public ViewKind ViewKind { get; }

        public ListTreeNode(string name, int iconIndex, Array array, ViewKind viewKind, params NativeTreeNode[] children) : base(name, iconIndex, children)
        {
            Array = array;
            ViewKind = viewKind;
        }

        public ListTreeNode(string name, int iconIndex, int offset, int count, ViewKind viewKind, params NativeTreeNode[] children) : base(name, iconIndex, children)
        {
            Offset = offset;
            Count = count;
            ViewKind = viewKind;
        }
    }
}
