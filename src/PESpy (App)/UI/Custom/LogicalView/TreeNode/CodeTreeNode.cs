using System;

namespace PESpy.UI
{
    /// <summary>
    /// Represents a tree node that when opened causes the <see cref="TextView"/> to navigate to code.
    /// </summary>
    class CodeTreeNode : LogicalTreeNode
    {
        public override LogicalTreeNodeKind Kind => LogicalTreeNodeKind.Code;

        public int Offset { get; }

        public CodeTreeNode(string name, int iconIndex, int offset) : base(name, iconIndex, Array.Empty<NativeTreeNode>())
        {
            Offset = offset;
        }
    }
}
