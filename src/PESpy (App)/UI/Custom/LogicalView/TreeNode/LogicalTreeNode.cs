using System.Diagnostics;

namespace PESpy.UI
{
    abstract class LogicalTreeNode : NativeTreeNode
    {
        public abstract LogicalTreeNodeKind Kind { get; }

        public bool HasContent
        {
            get
            {
                switch (Kind)
                {
                    case LogicalTreeNodeKind.Overview:
                    case LogicalTreeNodeKind.Singleton:
                    case LogicalTreeNodeKind.List:
                    case LogicalTreeNodeKind.Code:
                        return true;

                    case LogicalTreeNodeKind.Folder:
                        return false;

                    default:
                        Debug.Assert(false);
                        return default;
                }
            }
        }

        public new LogicalTreeNode FirstNode => (LogicalTreeNode) base.FirstNode;

        protected LogicalTreeNode(string name, int iconIndex, NativeTreeNode[] children) : base(name, iconIndex, iconIndex, children)
        {
        }
    }
}
