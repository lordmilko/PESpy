namespace PESpy.UI
{
    /// <summary>
    /// Represents a tree node that displays as a folder.
    /// </summary>
    class FolderTreeNode : LogicalTreeNode
    {
        public override LogicalTreeNodeKind Kind => LogicalTreeNodeKind.Folder;

        public FolderTreeNode(string name, int iconIndex, NativeTreeNode[] children) : base(name, iconIndex, children)
        {
        }
    }
}
