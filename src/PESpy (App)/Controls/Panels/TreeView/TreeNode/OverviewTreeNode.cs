using System;
#if WINFORMS
using System.Windows.Forms;
#endif
using PESpy.Controls;

namespace PESpy
{
    /// <summary>
    /// Represents a tree node that when opened triggers the <see cref="OverviewPanel"/> to be displayed.
    /// </summary>
    class OverviewTreeNode : TreeNodeEx
    {
        public override TreeNodeKind Kind => TreeNodeKind.Overview;

        public OverviewTreeNode(string name, int iconIndex) : base(name, iconIndex, Array.Empty<TreeNode>())
        {
        }
    }
}
