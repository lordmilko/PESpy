#if WINFORMS
using System.Windows.Forms;
#else
using TreeNode = PESpy.Controls.NativeTreeNode;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a tree node that displays as a folder.
    /// </summary>
    class FolderTreeNode : TreeNodeEx
    {
        public override TreeNodeKind Kind => TreeNodeKind.Folder;

        public FolderTreeNode(string name, int iconIndex, TreeNode[] children) : base(name, iconIndex, children)
        {
        }
    }
}
