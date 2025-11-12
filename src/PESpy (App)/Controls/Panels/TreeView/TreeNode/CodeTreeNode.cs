using System;
#if WINFORMS
using System.Windows.Forms;
#else
using TreeNode = PESpy.Controls.NativeTreeNode;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a tree node that when opened causes the <see cref="TextView"/> to navigate to code.
    /// </summary>
    class CodeTreeNode : TreeNodeEx
    {
        public override TreeNodeKind Kind => TreeNodeKind.Code;

        public int Offset { get; }

        public CodeTreeNode(string name, int iconIndex, int offset) : base(name, iconIndex, Array.Empty<TreeNode>())
        {
            Offset = offset;
        }
    }
}
