using System;
#if WINFORMS
using System.Windows.Forms;
#else
using TreeNode = PESpy.Controls.NativeTreeNode;
#endif

namespace PESpy
{
    abstract class TreeNodeEx : TreeNode
    {
        public abstract TreeNodeKind Kind { get; }

        public bool HasContent
        {
            get
            {
                switch (Kind)
                {
                    case TreeNodeKind.Overview:
                    case TreeNodeKind.Singleton:
                    case TreeNodeKind.List:
                    case TreeNodeKind.Code:
                        return true;

                    case TreeNodeKind.Folder:
                        return false;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public new TreeNodeEx FirstNode => (TreeNodeEx) base.FirstNode;

        protected TreeNodeEx(string name, int iconIndex, TreeNode[] children) : base(name, iconIndex, iconIndex, children)
        {
        }
    }
}
