using System;

namespace PESpy.UI
{
    /// <summary>
    /// Represents a tree node that when opened triggers a special pane such as the the <see cref="OverviewPanel"/> to be displayed.
    /// </summary>
    class SpecialPaneNode : LogicalTreeNode
    {
        public override LogicalTreeNodeKind Kind { get; }

        public SpecialPaneNode(string name, int iconIndex, LogicalTreeNodeKind kind) : base(name, iconIndex, Array.Empty<NativeTreeNode>())
        {
            Kind = kind;
        }
    }
}
