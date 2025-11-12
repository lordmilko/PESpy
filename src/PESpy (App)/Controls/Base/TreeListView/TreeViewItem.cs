#if WINFORMS
using System.Windows.Forms;
#else
using ListViewItem = PESpy.Controls.NativeListViewItem;
using ListViewSubItem = PESpy.Controls.NativeListViewItem.NativeListViewSubItem;
#endif

namespace PESpy.Controls
{
    class TreeViewItem : ListViewItem
    {
        public TreeViewItem? Parent { get; internal set; }

        private TreeViewItemCollection? nodes;

        public TreeViewItemCollection Nodes
        {
            get
            {
                if (nodes == null)
                    nodes = new TreeViewItemCollection(this);

                return nodes;
            }
        }

        //Used to avoid allocating Nodes unnecessarily
        public bool HasChildren => (nodes != null && nodes.Count > 0) || HasVirtualChildren;

        public bool HasVirtualChildren { get; set; }

        public int Level => Parent == null ? 0 : Parent.Level + 1;

        //When the user expands the control, IsExpanded will be set to true, so we can't be calling Expand
        //again. Thus, we have a separate method called Expand() that you can call directly
        public bool IsExpanded { get; internal set; }

        public TreeViewItem(string item)
        {
            SubItems[0] = new TreeViewSubItem(this, item);
        }

        public TreeViewItem(string?[] items)
        {
            var newSubItems = new TreeViewSubItem[items.Length - 1];

            //Upon touching SubItems, the ListViewItem will turn into a listview subitem, so we'll go ahead and replace it with a treeview subitem
            var subItems = SubItems;
            subItems[0] = new TreeViewSubItem(this, items[0]!);

            for (var i = 1; i < items.Length; i++)
                newSubItems[i - 1] = new TreeViewSubItem(this, items[i]!);

            subItems.AddRange(newSubItems);
        }

        public TreeViewSubItem Add(string text) =>
            (TreeViewSubItem) SubItems.Add(new TreeViewSubItem(this, text));

        public class TreeViewSubItem : ListViewSubItem
        {
            internal int ImageIndex { get; set; } = -1;

            internal bool IsLink { get; set; }

            internal bool IsLinkActive { get; set; }

            public TreeViewSubItem(TreeViewItem owner, string text) : base(owner, text)
            {
            }
        }
    }
}
