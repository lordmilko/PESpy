using System;
#if WINFORMS
using System.Windows.Forms;
#endif
using PESpy.Controls;

namespace PESpy
{
    /// <summary>
    /// Represents a tree node that when opened triggers a special pane such as the the <see cref="OverviewPanel"/> to be displayed.
    /// </summary>
    class SpecialPaneNode : TreeNodeEx
    {
        public override TreeNodeKind Kind { get; }

        public SpecialPaneNode(string name, int iconIndex, TreeNodeKind kind) : base(name, iconIndex, Array.Empty<TreeNode>())
        {
            Kind = kind;
        }
    }
}
