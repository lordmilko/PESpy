using System;
using System.Collections.Generic;
using System.Diagnostics;
using PInvoke;

namespace PESpy.UI
{
    /// <summary>
    /// Represents a lightweight wrapper around the native SysTreeView32 TreeView control.
    /// </summary>
    public class NativeTreeView : SystemWindow
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ClassName = "SysTreeView32";

                createParams.Style |= (int) (TVS.TVS_HASLINES | TVS.TVS_HASBUTTONS | TVS.TVS_SHOWSELALWAYS);

                return createParams;
            }
        }

        public NativeImageList ImageList { get; set; }

        public NativeTreeNodeCollection Nodes => _root.Nodes;

        //This field seems to be used by WinForms for storing the selected node when the handle hasn't been created yet
        private NativeTreeNode? _selectedNode;

        public NativeTreeNode? SelectedNode
        {
            get
            {
                if (IsHandleCreated)
                {
                    var hItem = User32.SendMessageW(hWnd, (int) TVM.TVM_GETNEXTITEM, (nint) TVGN.TVGN_CARET, default);

                    if (hItem == default)
                        return null;

                    return _handleToNodeMap[hItem];
                }
                else if (_selectedNode != null)
                    return _selectedNode;

                return null;
            }
            set
            {
                if (IsHandleCreated)
                {
                    LPARAM lParam;

                    if (value == null)
                        lParam = default;
                    else
                    {
                        Debug.Assert(value.hTreeItem != default);
                        lParam = (nint) value.hTreeItem;
                    }

                    Debug.Assert(Height != 0, "Setting the selected node before the height has been set will cause the native TreeView to try and scroll the selected item into view (so much so it goes behind the actual top of the screen)");

                    /* Sending the TVM_SELECTITEM message results in an WM_NOTIFY message
                     * being sent. The behavior of WM_NOTIFY messages is that they are
                     * sent to the _parent_, not the child control itself (as typically the
                     * child control won't have been subclassed). As such, the behavior of WinForms
                     * is to intercept these WM_NOTIFY messages and "reflect" them back towards
                     * the child control */
                    User32.SendMessageW(hWnd, (int) TVM.TVM_SELECTITEM, (int) TVGN.TVGN_CARET, (nint) (value == null ? 0 : lParam));
                    _selectedNode = null;
                }
                else
                {
                    _selectedNode = value;
                }
            }
        }

        internal NativeTreeNode _root;
        private bool _stopResizeMessages;

        //Use IntPtr so we don't have to bring in code for hashing the HTREEITEM in NativeAOT
        internal Dictionary<IntPtr, NativeTreeNode> _handleToNodeMap = new();

        public event EventHandler<NativeTreeNode?> AfterSelect;

        public NativeTreeView()
        {
            _root = new NativeTreeNode(this);
            Border = true;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM.WM_WINDOWPOSCHANGING:
                case WM.WM_NCCALCSIZE:
                case WM.WM_WINDOWPOSCHANGED:
                case WM.WM_SIZE:

                    if (_stopResizeMessages)
                    {
                        DefWndProc(ref m);
                        return;
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        protected override void OnHandleCreated()
        {
            var oldSelectedNode = _selectedNode;
            _selectedNode = null;

            User32.SendMessageW(hWnd, (int) TVM.TVM_SETEXTENDEDSTYLE, (int) TVS_EX.TVS_EX_DOUBLEBUFFER, (int) TVS_EX.TVS_EX_DOUBLEBUFFER);

            /* A bit of a gotcha with the image list is that it seems to need to have all of its images in it _prior_ to calling TVM_SETIMAGELIST.
             * I was having issues wherein half the time I would have images, half the time I wouldn't! The big difference between us and WinForms
             * seems to be the fact that when you set the Size property, our NativeToWinFormsHostControl.CreateParams will be called upon, which
             * may touch the Parent.Handle, thereby force loading the parent panel, which in turn will force load the current control, before the ImageList
             * has potentially been initialized. WinForms doesn't have this issue because it uses a property InternalHandle which, unlike the regular
             * Handle property, does _not_ force create the control */
            if (ImageList != null)
            {
                User32.SendMessageW(hWnd, (int) TVM.TVM_SETIMAGELIST, (int) TVSIL.TVSIL_NORMAL, (nint) ImageList.hImageList);
            }

            _stopResizeMessages = true;

            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE;

            var oldWidth = Width;
            User32.SetWindowPos(hWnd, default, Left, Top, int.MaxValue, Height, flags);

            _root.Realize(false);

            User32.SetWindowPos(hWnd, default, Left, Top, Width, Height, flags);
            _stopResizeMessages = false;

            //Now that the handle has been created, we can apply the selected node (if any node was selected prior to the handle being initialized)
            SelectedNode = oldSelectedNode;
        }

        protected override unsafe void WmReflectNotify(ref Message m, NMHDR* nmhdr)
        {
            var nmtv = (NMTREEVIEWW*) nmhdr;

            var code = (TVN) nmtv->hdr.code;

            switch (code)
            {
                case TVN.TVN_ITEMEXPANDINGW:
                    if (nmtv->itemNew.hItem != 0)
                    {
                        _handleToNodeMap.TryGetValue(nmtv->itemNew.hItem, out var node);

                        if ((nmtv->itemNew.state & TREE_VIEW_ITEM_STATE_FLAGS.TVIS_EXPANDED) == 0)
                            OnBeforeExpand(node);
                        else
                            OnBeforeCollapse(node);
                    }
                    break;

                case TVN.TVN_ITEMEXPANDEDW:
                    if (nmtv->itemNew.hItem != 0)
                    {
                        _handleToNodeMap.TryGetValue(nmtv->itemNew.hItem, out var node);

                        if ((nmtv->itemNew.state & TREE_VIEW_ITEM_STATE_FLAGS.TVIS_EXPANDED) == 0)
                            OnAfterCollapse(node);
                        else
                            OnAfterExpand(node);
                    }
                    break;

                case TVN.TVN_SELCHANGEDW:
                    if (nmtv->itemNew.hItem != 0)
                    {
                        _handleToNodeMap.TryGetValue(nmtv->itemNew.hItem, out var node);

                        AfterSelect?.Invoke(this, node);
                    }
                    break;
            }
        }

        //Double buffering the TreeView fixes the items themselves flickering when you expand/collapse
        //the view, however when a bunch of stuff is already expanded, it might disappear over multiple frames,
        //which itself a different kind of flicker
        private void OnBeforeCollapse(NativeTreeNode? node) => BeginUpdate();

        private void OnAfterCollapse(NativeTreeNode? node) => EndUpdate();

        private void OnBeforeExpand(NativeTreeNode? node) => BeginUpdate();

        private void OnAfterExpand(NativeTreeNode? node) => EndUpdate();
    }
}
