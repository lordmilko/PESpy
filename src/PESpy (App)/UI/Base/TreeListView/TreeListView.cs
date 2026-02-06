using System;
using System.Collections.Generic;
using System.Drawing;
using PInvoke;
using static PInvoke.Macros;

namespace PESpy.UI
{
    internal class TreeListView : NativeListView
    {
        /* About Custom Draw
         * https://learn.microsoft.com/en-us/windows/win32/controls/about-custom-draw
         * 
         * Custom draw messages are automatically sent during painting. However, there is also "owner drawing",
         * wherein you specify window style LVS_OWNERDRAWFIXED and then respond to WM_DRAWITEM and WM_MEASUREITEM
         * messages
         * 
         * It is distinct from "owner draw". In "owner draw", you set LVS_OWNERDRAWFIXED in your window style,
         * wherein you're then responsible for drawing everything. Setting LVS_OWNERDRAWFIXED will cause
         * WM_MEASUREITEM and WM_DRAWITEM messages to be sent to you
         * 
         * Technically speaking you can draw whatever you like during custom draw however, so it's not clear to me why
         * why you would use "owner draw", or if "owner draw" is perhaps a legacy thing from before "custom draw"
         * came about */

        private const int LEVEL_INDENTATION = 19;
        private const int GLYPH_SPACE = 18;

        private int _rootGlyphSpace = GLYPH_SPACE;
        private SIZE _glyphSize;

        internal HFONT _hFont;
        private HFONT _hHyperLinkFont;
        private HTHEME _hTheme;
        private HBRUSH _hRowBrush; //Stock brush
        private HBRUSH _hAlternateRowBrush;

        private TreeViewItem.TreeViewSubItem? _currentHyperLink;

        private int _lastClientWidth;

        private int _expansionDepth;

        internal bool CanShowExpander
        {
            get => _rootGlyphSpace == GLYPH_SPACE;
            set => _rootGlyphSpace = value ? GLYPH_SPACE : 0;
        }

        internal const int CONTENT_LEFT_OFFSET = 4;
        internal const int LABEL_LEFT_OFFSET = 2;
        internal const int IMAGE_TEXT_GAP = 5;

        public event EventHandler<TreeListViewCancelEventArgs>? BeforeCollapse;
        public event EventHandler<TreeListViewCancelEventArgs>? BeforeExpand;

        //While ListView may support displaying images in subitems using the LVM_SETEXTENDEDLISTVIEWSTYLE,
        //we're already so full custom here, that actually limits us, because when we add virtual
        //children we want to be able to assign them images as well, which is difficult
        //when we don't track where they'll be inserted at. Thus, we just maintain our image indices
        //ourself

        protected unsafe override void OnHandleCreated()
        {
            var lf = new LOGFONTW();

            User32.SystemParametersInfoW(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETICONTITLELOGFONT, sizeof(LOGFONTW), (void*) (&lf), 0);

            _hFont = Gdi32.CreateFontIndirectW(lf);

            lf.lfUnderline = 1;
            _hHyperLinkFont = Gdi32.CreateFontIndirectW(lf);

            _hTheme = UxTheme.OpenThemeData(hWnd, "TreeView");

            var hdc = User32.GetDCEx(hWnd, default, GET_DCX_FLAGS.DCX_CACHE);

            _hRowBrush = Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH);
            _hAlternateRowBrush = Gdi32.CreateSolidBrush(new COLORREF(0xF3, 0xF3, 0xF3));

            SIZE glyphSize;
            UxTheme.GetThemePartSize(_hTheme, hdc, (int) TREEVIEWPARTS.TVP_GLYPH, (int) GLYPHSTATES.GLPS_OPENED, default, THEMESIZE.TS_TRUE, &glyphSize);
            _glyphSize = glyphSize;

            User32.ReleaseDC(hWnd, hdc);
        }

        protected override void OnHandleDestroyed()
        {
            if (_hFont != default)
            {
                Gdi32.DeleteObject(_hFont);
                _hFont = default;
            }

            if (_hHyperLinkFont != default)
            {
                Gdi32.DeleteObject(_hHyperLinkFont);
                _hHyperLinkFont = default;
            }

            if (_hTheme != default)
            {
                UxTheme.CloseThemeData(_hTheme);
                _hTheme = default;
            }

            if (_hAlternateRowBrush != default)
            {
                Gdi32.DeleteObject(_hAlternateRowBrush);
                _hAlternateRowBrush = default;
            }
        }

        protected unsafe override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM.WM_REFLECT_NOTIFY:
                    //Returns true if we handle this
                    if (WmReflectNotify((NMHDR*) (nint) m.lParam, out var cdrf))
                    {
                        m.Result = (LRESULT) (nint) cdrf;
                        return;
                    }

                    break;

                case WM.WM_LBUTTONDOWN:
                case WM.WM_LBUTTONDBLCLK:
                    //If the user clicks too quickly, WM_LBUTTONDOWN will be sent on the first click, and on the second we'll get a WM_LBUTTONDBLCLK
                    //so just treat it like a normal click
                    if (WmMouseDown((short) LOWORD((uint) m.lParam), (short) HIWORD((uint) m.lParam)))
                        return;

                    break;
            }

            base.WndProc(ref m);
        }

        #region DrawSubItem

        private void DrawSubItem(
            HDC hdc,
            int itemIndex,
            int subItemIndex,
            TreeViewItem treeViewItem,
            TreeViewItem.TreeViewSubItem subItem,
            in Rectangle bounds)
        {
            //It's important to use these bounds and not the bounds of the subitem; these bounds
            //have been adjusted for the fact that the first subitem has bounds that span the entire
            //width of the row

            var image = subItem.ImageIndex == -1 ? null : ImageList[subItem.ImageIndex];

            Gdi32.SelectObject(hdc, subItem.IsLinkActive ? _hHyperLinkFont : _hFont);

            var textColor = subItem.ForeColor;

            if (subItem.IsLink)
                textColor = new COLORREF(0, 0, 255); //Blue

            if (treeViewItem.Selected)
                DrawSelectedSubItem(hdc, subItemIndex, treeViewItem, subItem, ref textColor, bounds);
            else
                DrawNormalSubItem(hdc, itemIndex, bounds);

            var treeViewTextOffset = 0;

            if (subItemIndex == 0)
                treeViewTextOffset = _rootGlyphSpace + (treeViewItem.Level * LEVEL_INDENTATION);

            DrawString(hdc, textColor, subItem.Text, image, bounds, treeViewTextOffset, out var imageX);
            DrawImage(hdc, image, imageX, bounds);
            DrawExpander(hdc, subItemIndex, treeViewItem, bounds);
        }

        private void DrawSelectedSubItem(
            HDC hdc,
            int subItemIndex,
            TreeViewItem treeViewItem,
            NativeListViewSubItem subItem,
            ref COLORREF textColor,
            Rectangle bounds)
        {
            textColor = GetSelectedForegroundColor(subItem);
            var bgColor = GetSelectedBackgroundColor(subItem);

            var hBrush = Gdi32.CreateSolidBrush(new COLORREF(bgColor));

            var off = CONTENT_LEFT_OFFSET - 2; //-2 because when there's a TreeView glyph, we're too close to the edge of the background when we're selected

            bounds.X += off;

            //If this is the last column, don't use the full width, as we'll go over the edge to the outside
            bounds.Width = subItemIndex == treeViewItem.SubItems.Count - 1
                ? bounds.Width - off
                : bounds.Width;

            User32.FillRect(hdc, bounds, hBrush);

            Gdi32.DeleteObject(hBrush);
        }

        private void DrawNormalSubItem(HDC hdc, int itemIndex, Rectangle bounds)
        {
            if ((itemIndex % 2) == 1)
                User32.FillRect(hdc, new RECT(bounds), _hAlternateRowBrush);
            else
            {
                bounds.Inflate(-1, -1); //Protect the grid lines
                User32.FillRect(hdc, new RECT(bounds), _hRowBrush); //We need to make sure we paint this, else when the last row is only partially visible, it won't get painted over and the text will double up
            }
        }

        internal static unsafe void DrawString(
            HDC hdc,
            COLORREF color,
            string text,
            NativeBitmap? image,
            Rectangle bounds,
            int treeViewTextOffset,
            out int imageX)
        {
            Gdi32.SetTextColor(hdc, color);
            Gdi32.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);

            var textX = bounds.Left + CONTENT_LEFT_OFFSET + LABEL_LEFT_OFFSET + treeViewTextOffset;
            imageX = textX;

            if (image != null)
                textX += image.Width + IMAGE_TEXT_GAP;

            //ExtTextOutW with ETO_CLIPPED does not add an ellipsis for any clipped text,
            //so we must use DrawTextW instead

            var point = new Point(textX, bounds.Top + 2);

            var r = new RECT
            {
                top = bounds.Top + (point.Y - bounds.Top), //When we were using ExtTextOutW you could specify a point where the text should begin. Merge that point into the rect
                left = bounds.Left + (point.X - bounds.Left),
                right = bounds.Right,
                bottom = bounds.Bottom
            };

            fixed (char* p = text)
                User32.DrawTextW(hdc, (PCWSTR) p, text.Length, &r, DRAW_TEXT_FORMAT.DT_WORD_ELLIPSIS);
        }
        private void DrawExpander(
            HDC hdc,
            int subItemIndex,
            TreeViewItem treeViewItem,
            in Rectangle bounds)
        {
            if (subItemIndex == 0 && treeViewItem.HasChildren) //Nodes allocates a list if there isn't one already, so we use HasChildren instead
            {
                var rect = GetGlyphRect(treeViewItem.Level, bounds);

                var stateId = treeViewItem.IsExpanded ? GLYPHSTATES.GLPS_OPENED : GLYPHSTATES.GLPS_CLOSED;
                UxTheme.DrawThemeBackground(_hTheme, hdc, (int) TREEVIEWPARTS.TVP_GLYPH, (int) stateId, rect, null);
            }
        }

        private Rectangle GetGlyphRect(int level, in Rectangle subItemBounds)
        {
            var gap = subItemBounds.Height - _glyphSize.Height;
            var topGap = (int) Math.Ceiling((double) gap / 2);

            var rect = new Rectangle(
                subItemBounds.Left + 7 + (LEVEL_INDENTATION * level),
                subItemBounds.Top + topGap,
                _glyphSize.Width,
                _glyphSize.Height
            );

            return rect;
        }
            if (subItemIndex == 0)
            {
                //The hit box of the expander is actually the entire area surrounding the glyph, (not including the level padding) right up until the start of the label
                var glyphHitBox = bounds;
                glyphHitBox.X += (LEVEL_INDENTATION * treeViewItem.Level);
                glyphHitBox.Width = GLYPH_SPACE + CONTENT_LEFT_OFFSET + LABEL_LEFT_OFFSET; //The width up to the start of the label

                if (glyphHitBox.Contains(new Point(x, y)))
                {
                    if (!treeViewItem.HasChildren)
                        return false; //We clicked in the area where there might have been a glyph, but there isn't actually one there

                    ToggleExpanded(treeViewItem, bounds);
                    return false;
                }
            }

            return false;
        }

        protected override void WmMouseMove(ref Message m, int x, int y)
        {
            base.WmMouseLeave(ref m);

            var info = HitTest(x, y);

            var subItem = (TreeViewItem.TreeViewSubItem) info.SubItem;

            if (subItem == null)
            {
                if (_currentHyperLink != null)
                    ClearHyperLink();

                return;
            }

            //Is the item we hit a hyperlink?
            if (!subItem.IsLink)
                return;

            //Did we actually hit the text area?
            var textBounds = GetTextBounds(subItem);

            if (!textBounds.Contains(new Point(x, y)))
            {
                if (_currentHyperLink != null)
                    ClearHyperLink();
                return;
            }

            if (subItem == _currentHyperLink)
                return; //No change

            if (_currentHyperLink != null)
            {
                //There's going to be a hyperlink, just not the current one
                _currentHyperLink.IsLinkActive = false;
                Invalidate(_currentHyperLink.Bounds);
            }
        private unsafe Rectangle GetTextBounds(TreeViewItem.TreeViewSubItem subItem)
        {
            var bounds = subItem.Bounds;

            var hdc = User32.GetDCEx(hWnd, default, GET_DCX_FLAGS.DCX_CACHE);

            Gdi32.SelectObject(hdc, _hHyperLinkFont);

            fixed (char* ptr = subItem.Text)
            {
                SIZE size;
                Gdi32.GetTextExtentPoint32W(hdc, ptr, subItem.Text.Length, &size);

                //Calculate the starting X pos
                bounds.X += CONTENT_LEFT_OFFSET + LABEL_LEFT_OFFSET;

                if (subItem.ImageIndex != -1)
                    bounds.X += 16 + IMAGE_TEXT_GAP; //Images should be 16x16

                //If we can have hyperlinks in the first column, we'd need to factor in whether
                //we're the first level too
                bounds.Width = size.cx;
            }

            User32.ReleaseDC(hWnd, hdc);

            return bounds;
        }

        protected override void WmMouseLeave(ref Message m)
        {
            base.WmMouseLeave(ref m);

            if (_currentHyperLink != null)
                ClearHyperLink();
        }

        protected override void WmKeyDown(ref Message m, VIRTUAL_KEY key)
        {
            base.WmKeyDown(ref m, key);

            switch (key)
            {
                case VIRTUAL_KEY.VK_LEFT:
                    ProcessLeftKey();
                    break;

                case VIRTUAL_KEY.VK_RIGHT:
                    ProcessRightKey();
                    break;
            }
        }

        private void ProcessLeftKey()
        {
            SuspendLayout();

            if (SelectedItems.Count == 0)
                return;

            var firstSelectedItem = (TreeViewItem) SelectedItems[0];

            SuspendLayout();

            //If we're expanded, collapse us
            if (firstSelectedItem.IsExpanded)
            {
                ToggleExpanded(firstSelectedItem, firstSelectedItem.Bounds);
                firstSelectedItem.EnsureVisible();
            }
            else
            {
                if (firstSelectedItem.Parent != null)
                {
                    //If we're not expanded, move the selection to the item above us
                    SelectedItems.Clear();

                    firstSelectedItem.Parent.Selected = true;
                    firstSelectedItem.Parent.EnsureVisible();
                }
            }

            ResumeLayout();
        }

        private void ProcessRightKey()
        {
            if (SelectedItems.Count == 0)
                return;

            var firstSelectedItem = (TreeViewItem) SelectedItems[0];

            //If we have no children, do nothing
            if (!firstSelectedItem.HasChildren)
                return;

            SuspendLayout();

            //If we're collapsed, expand us
            if (!firstSelectedItem.IsExpanded)
            {
                ToggleExpanded(firstSelectedItem, firstSelectedItem.Bounds);
                firstSelectedItem.EnsureVisible();
            }

            ResumeLayout();
        }
        #region Expand / Collapse

        internal void ToggleExpanded(TreeViewItem treeViewItem) => ToggleExpanded(treeViewItem, treeViewItem.Bounds);

        private void ToggleExpanded(TreeViewItem treeViewItem, in Rectangle bounds)
        {
            SuspendLayout();

            if (treeViewItem.IsExpanded)
            {
                //Collapse!
                Collapse(treeViewItem, bounds);
            }
            else
            {
                //Expand!
                Expand(treeViewItem, bounds);
            }

            ResumeLayout();
        }

        internal void Expand(TreeViewItem treeViewItem, in Rectangle bounds)
        {
            _expansionDepth++;

            var eventArgs = new TreeListViewCancelEventArgs(treeViewItem, TreeViewAction.Expand);

            BeforeExpand?.Invoke(this, eventArgs);

            if (eventArgs.Cancel)
                return;

            treeViewItem.IsExpanded = true;

            //BeforeExpanding could cause the next level to want to eager expand itself as well. However, we don't want to add the next
            //level to the tree, as when we get back to the base level, we'll end up re-adding those nodes during AddDescendants which
            //will cause an issue
            if (_expansionDepth == 1)
            {
                //Some descendants may already be expanded from when their parent was expanded previously
                AddDescendants(treeViewItem, treeViewItem.Index + 1);

                //Redraw using the toggled glyph
                Invalidate(bounds);
            }

            _expansionDepth--;
        }

        internal void Collapse(TreeViewItem treeViewItem, in Rectangle bounds)
        {
            BeforeCollapse?.Invoke(this, new TreeListViewCancelEventArgs(treeViewItem, TreeViewAction.Collapse));

            //Remove all child nodes. If any of those nodes are expanded, we need to remove their children too
            RemoveDescendants(treeViewItem);

            treeViewItem.IsExpanded = false;

            //Redraw using the toggled glyph
            Invalidate(bounds);
        }

        private int AddDescendants(TreeViewItem item, int nextIndex)
        {
            //todo: this is not very efficient, because we are going to call setitemcount+1 multiple times adding just 1 at a time. i think the real solution here
            //is to make the whole thing virtual
            if (item.IsExpanded)
            {
                for (var i = 0; i < item.Nodes.Count; i++)
                {
                    var child = item.Nodes[i];

                    Items.Insert(nextIndex, child);
                    nextIndex++;

                    nextIndex = AddDescendants(child, nextIndex);
                }
            }

            return nextIndex;
        }

        private void RemoveDescendants(TreeViewItem item)
        {
            if (item.IsExpanded)
            {
                for (var i = item.Nodes.Count - 1; i >= 0; i--)
                {
                    var child = item.Nodes[i];
                    RemoveDescendants(child);
                    Items.Remove(child);
                }
            }
        }

        #endregion

        //Unlike the built-in AutoResizeColumns methods, this method can operate on items that haven't been added to the listview
        //yet and avoid all of the flicker that would occur while everything is repositioned (and it seemed that disabling redraw didn't help)
        public unsafe void AutoSizeColumns(IList<TreeViewItem> items)
        {
            //Measure the minimum required width of each column. Doing this once the items have been added will cause a whole bunch of flicker

            //Every row should have the same number of columns. For performance, we'll calculate just the number of chars in each item,
            //then try and approximate the actual width that would be required
            Span<int> columnWidths = stackalloc int[items[0].SubItems.Count];

            var columnHeaders = Columns;

            var hdc = User32.GetDCEx(hWnd, default, GET_DCX_FLAGS.DCX_CACHE);

            Gdi32.SelectObject(hdc, _hFont);

            //There's 6 pixels of padding either side of the header. Factor that in when calculating the larger of the
            //header width and its inner text
            const int headerPadding = 12;
            const int bodyPadding = 15; //Made up

            for (var i = 0; i < columnHeaders.Count; i++)
            {
                //If the column header is not properly sized, we should prefer the larger of its text or its actual width
                var header = columnHeaders[i];

                fixed (char* ptr = header.Text)
                {
                    SIZE size;
                    Gdi32.GetTextExtentPoint32W(hdc, ptr, header.Text.Length, &size);

                    columnWidths[i] = Math.Max(header.Width, size.cx + headerPadding);
                }
            }

            var isListMode = columnHeaders[0].Text == "#";

            for (var i = 0; i < items.Count; i++)
            {
                var listViewItem = items[i];

                for (var j = 0; j < listViewItem.SubItems.Count; j++)
                {
                    var subItem = (TreeViewItem.TreeViewSubItem) listViewItem.SubItems[j];

                    fixed (char* ptr = subItem.Text)
                    {
                        SIZE size;
                        Gdi32.GetTextExtentPoint32W(hdc, ptr, subItem.Text.Length, &size);

                        var myWidth = size.cx + CONTENT_LEFT_OFFSET + LABEL_LEFT_OFFSET + bodyPadding; //Add a little bit extra so the cell doesn't feel so tight

                        if (j == 0 && isListMode)
                            myWidth -= (bodyPadding / 2); //Don't include so much padding for the # column

                        if (subItem.ImageIndex != -1)
                            myWidth += 16 + IMAGE_TEXT_GAP; //Images should be 16x16

                        columnWidths[j] = Math.Max(columnWidths[j], myWidth);
                    }
                }
            }

            User32.ReleaseDC(hWnd, hdc);

            var lpsi = new SCROLLINFO
            {
                cbSize = sizeof(SCROLLINFO),
                fMask = SCROLLINFO_MASK.SIF_ALL
            };

            User32.GetScrollInfo(hWnd, SCROLLBAR_CONSTANTS.SB_VERT, ref lpsi);
            var lastColumnWidth = ClientSize.Width - User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVSCROLL);

            const int maxWidth = 400;

            for (var i = 0; i < columnHeaders.Count; i++)
            {
                if (i < columnHeaders.Count - 1)
                {
                    var columnWidth = columnWidths[i];

                    lastColumnWidth -= columnWidth;

                    columnHeaders[i].Width = Math.Min(columnWidth, maxWidth);
                }
                else
                {
                    //The last column gets all of the remaining width, unless we're off
                    //the screen in which case it gets its desired width
                    var lastWidth = Math.Max(lastColumnWidth, Math.Min(columnWidths[i], maxWidth));

                    columnHeaders[i].Width = lastWidth;
                }
            }
        }
        protected override void WmWindowPosChanged(ref Message m)
        {
            //If we resize in OnResize, the resize event will have already been passed
            //to the DefWndProc, and if we're shrinking the horizontal size, a vertical scrollbar
            //will temporarily appear and then disappear. By updating the width prior to calling
            //DefWndProc, the size has already been corrected and a scrollbar never appears

            if (Columns.Count > 0)
            {
                var diff = ClientSize.Width - _lastClientWidth;

                if (diff != 0)
                {
                    Columns[Columns.Count - 1].Width = -2;
                }
            }

            _lastClientWidth = ClientSize.Width;

            base.WmWindowPosChanged(ref m);
        }
    }
}
