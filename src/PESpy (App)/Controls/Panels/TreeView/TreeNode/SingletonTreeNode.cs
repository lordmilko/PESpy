#if WINFORMS
using System.Windows.Forms;
#endif
using PESpy.View;
#if !WINFORMS
using TreeNode = PESpy.Controls.NativeTreeNode;
#endif

namespace PESpy
{
    class SingletonTreeNode : TreeNodeEx
    {
        public override TreeNodeKind Kind => TreeNodeKind.Singleton;

        //Gets the offset of this struct; when passed to the FileAccessor, can be used to construct a view
        //around this object in order to display it within a table
        public int Offset { get; }

        public ViewKind ViewKind { get; }

        public SingletonTreeNode(
            string name,
            int iconIndex,
            int offset,
            ViewKind viewKind,
            TreeNode[] children) : base(name, iconIndex, children)
        {
            Offset = offset;
            ViewKind = viewKind;
        }
    }
}
