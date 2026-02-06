using PInvoke;

namespace PESpy.UI
{
    public class NativeTreeNode
    {
        public string Text { get; }

        public int ImageIndex { get; }

        public NativeTreeNode FirstNode => Nodes[0];

        internal NativeTreeNode? _parent;

        public NativeTreeNode? Parent
        {
            get
            {
                //The design of the WinForms TreeView is there is a top level "virtual" root
                //that the actual "visible" root(s) get added to. As such, this top level root
                //needs to be hidden from the external API

                var treeView = TreeView;

                if (treeView != null && _parent == treeView._root)
                    return null;

                return _parent;
            }
        }
        public HTREEITEM hTreeItem { get; private set; }

        private NativeTreeView _treeView;

        public NativeTreeView? TreeView =>
            _treeView ??= FindTreeView();

        private unsafe TREE_VIEW_ITEM_STATE_FLAGS State
        {
            get
            {
                if (hTreeItem == default)
                    return 0;

                var treeView = TreeView;

                if (treeView == null)
                    return default;

                var item = new TVITEMW
                {
                    hItem = hTreeItem,
                    mask = TVITEM_MASK.TVIF_HANDLE | TVITEM_MASK.TVIF_STATE,
                    stateMask = TREE_VIEW_ITEM_STATE_FLAGS.TVIS_BOLD | TREE_VIEW_ITEM_STATE_FLAGS.TVIS_SELECTED | TREE_VIEW_ITEM_STATE_FLAGS.TVIS_EXPANDED
                };

                User32.SendMessageW(treeView.hWnd, (int) TVM.TVM_GETITEMW, default, (nint) (&item));

                return item.state;
            }
        }

        internal bool IsExpanded => hTreeItem == default ? _expandOnRealization : (State & TREE_VIEW_ITEM_STATE_FLAGS.TVIS_EXPANDED) != 0;

        private NativeTreeNodeCollection _nodes;

        public NativeTreeNodeCollection Nodes => _nodes ??= new NativeTreeNodeCollection(this);

        private bool _expandOnRealization;
        internal int _index;

        public NativeTreeNode(NativeTreeView treeView)
        {
            _treeView = treeView;
            Nodes = new NativeTreeNodeCollection(this);
        }
        public void ExpandAll()
        {
            Expand();

            var nodes = Nodes;

            for (var i = 0; i < nodes.Count; i++)
                nodes[i].ExpandAll();
        }

        public unsafe void Expand()
        {
            var treeView = TreeView;

            if (treeView == null || !treeView.IsHandleCreated)
            {
                _expandOnRealization = true;
                return;
            }

            //WinForms does this. Based on reading MSDN, we won't get TVN_ITEMEXPANDING/TVN_ITEMEXPANDED notifications again
            //without clearing this flag?
            //https://learn.microsoft.com/en-us/windows/win32/controls/tree-view-control-item-states

            var item = new TVITEMW
            {
                mask = TVITEM_MASK.TVIF_HANDLE | TVITEM_MASK.TVIF_STATE,
                hItem = hTreeItem,
                stateMask = TREE_VIEW_ITEM_STATE_FLAGS.TVIS_EXPANDEDONCE,
                state = 0
            };

            User32.SendMessageW(treeView.hWnd, (int) TVM.TVM_SETITEMW, 0, (nint) (&item));

            if (!IsExpanded)
                User32.SendMessageW(treeView.hWnd, (int) TVM.TVM_EXPAND, (int) TVE.TVE_EXPAND, (nint) hTreeItem);

            _expandOnRealization = false;
        }

        internal unsafe void Realize(bool insertFirst)
        {
            var treeView = TreeView;

            if (treeView == null || !treeView.IsHandleCreated)
                return;

            if (_parent != null)
            {
                var tvis = new TVINSERTSTRUCTW
                {
                    hParent = _parent.hTreeItem
                };
                tvis.item.mask = TVITEM_MASK.TVIF_TEXT;
                tvis.hInsertAfter = HTREEITEM.TVI_FIRST;

                if (ImageIndex != -1)
                {
                    tvis.item.iImage = ImageIndex;
                    tvis.item.iSelectedImage = ImageIndex;
                    tvis.item.mask |= TVITEM_MASK.TVIF_IMAGE | TVITEM_MASK.TVIF_SELECTEDIMAGE;
                }

                fixed (char* c = Text)
                {
                    tvis.item.pszText = c;

                    hTreeItem = (HTREEITEM) (nint) User32.SendMessageW(treeView.hWnd, (int) TVM.TVM_INSERTITEMW, default, (nint) (&tvis));
                    treeView._handleToNodeMap[hTreeItem] = this;
                }
            }

            //WinForms realizes nodes backwards

            for (var i = Nodes.Count - 1; i >= 0; i--)
                Nodes[i].Realize(true);

            if (_expandOnRealization)
                Expand();
        }

        private NativeTreeView? FindTreeView()
        {
            var current = this;

            //While it's possible that any node along the pathway will have their TreeView set,
            //if anyone does it's the root, so it's faster to just go to the top rather than ask each
            //node along the way
            while (current._parent != null)
                current = current._parent;

            return current._treeView;
        }
    }
}
