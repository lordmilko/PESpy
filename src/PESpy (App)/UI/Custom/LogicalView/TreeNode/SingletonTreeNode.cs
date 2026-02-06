using PESpy.View;

namespace PESpy.UI
{
    class SingletonTreeNode : LogicalTreeNode
    {
        public override LogicalTreeNodeKind Kind => LogicalTreeNodeKind.Singleton;

        //Gets the offset of this struct; when passed to the FileAccessor, can be used to construct a view
        //around this object in order to display it within a table
        public int Offset { get; }

        public ViewKind ViewKind { get; }

        public SingletonTreeNode(
            string name,
            int iconIndex,
            int offset,
            ViewKind viewKind,
            NativeTreeNode[] children) : base(name, iconIndex, children)
        {
            Offset = offset;
            ViewKind = viewKind;
        }
    }
}
