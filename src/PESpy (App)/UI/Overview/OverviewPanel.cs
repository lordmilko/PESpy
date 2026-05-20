using System;
using System.Diagnostics;
using PInvoke;
using static PInvoke.DRAW_TEXT_FORMAT;
using static PInvoke.DTTOPTS_FLAGS;
using ReView;
using System.Diagnostics.CodeAnalysis;

namespace PESpy.Overview
{
    public class OverviewPanel : UIElement
    {
        private HTHEME _hTheme;
        private int _lastToolTipPos = -1;

        [AllowNull]
        private NativeFont _font; //Don't dispose
        private int _lineHeight;
        private OverviewGroup[]? _groups;
        private MaxColumnWidths? _maxColumnWidths; //Given all rows in a column, what is the largest value we need to represent in that column in order to line up the column after it
        private ColumnBounds _columnBounds; //Given the actual amount of visible space, how large should each column be. The width of a given column is the distance between one column and the one after it (factoring in the divider in the middle)
        private int _dividerXPos;

        private int _xPad;
        private int _yPad;

        private int _scrollXPos;
        private int _scrollYPos;
        private int _scrollHeight;
        private int _scrollWidth;

        private (int group, int row, int column) _selectedItem = new (-1, -1, -1);

        private static readonly COLORREF _grayColor = new COLORREF(0x8a8a8a);

        private const int DIVIDER_GAP = 10;
        private const int HEADER_BOTTOM_GAP = 4;

        public OverviewPanel(out OverviewPanel field)
        {
            field = this;
            Dock = DockStyle.Fill;

            App.FileOpened += App_FileOpened;
        }

        #region Layout

        private void App_FileOpened(object? sender, FileOpenedEventArgs e)
        {
            switch (e.EventKind)
            {
                case FileOpenedEventKind.OpenAccessor:
                    Window.BeginInvoke(() =>
                    {
                        using var scope = App.AcquireFileAccessor();

                        if (scope.FileAccessor == null)
                            return;

                        var overview = scope.FileAccessor.Overview;

                        switch (scope.FileAccessor.File.Kind)
                        {
                            case FileKind.PE:
                                ProcessPEFile((PEFileOverview) overview);
                                break;

                            case FileKind.PDB:
                                ProcessPDBFile((PDBFileOverview) overview);
                                break;

                            default:
                                throw new NotImplementedException();
                        }
                    });

                    break;

                case FileOpenedEventKind.AnalysisComplete:
                    /* We don't try and load symbols while constructing our FileAccessor; now that analysis has completed,
                     * symbols have been installed. As such, we may need to refresh the overview to reflect the fact
                     * that we now have symbols. This will affect any external paths to symbols as well as any values that
                     * contain symbols themselves */

                    using (var scope = App.AcquireFileAccessor())
                    {
                        if (scope.FileAccessor == null)
                            return;

                        scope.FileAccessor.RefreshOverviewSymbols();

                        //Do a final refresh to make sure we're all up to date
                        Invalidate();
                    }

                    break;
            }
        }

        #region PEFile

        private unsafe void ProcessPEFile(PEFileOverview overview)
        {
            var groups = new ValueList<OverviewGroup>();

            try
            {
                ProcessArchitectureVersion(overview, ref groups);
                ProcessEntryPointRichHeader(overview, ref groups);
                ProcessManaged(overview, ref groups);
                ProcessDebug(overview, ref groups);

                MeasureGroups(ref groups);
            }
            finally
            {
                groups.Dispose();
            }
        }

        private void MeasureGroups(ref ValueList<OverviewGroup> groups)
        {
            //We don't need to draw anything; we just need this for measuring text
            var hdc = User32.GetDCEx(NativeHandle, default, GET_DCX_FLAGS.DCX_CACHE);

            Gdi32.SelectObject(hdc, _font.hFont);

            //Now measure the required size of each column
            var maxColumnWidths = new MaxColumnWidths();

            var groupsSpan = groups.Span;

            var numRows = 0;

            for (var i = 0; i < groups.Count; i++)
            {
                ref var group = ref groupsSpan[i];
                numRows += group.Rows.Length;

                for (var j = 0; j < group.Rows.Length; j++)
                {
                    ref var row = ref group.Rows[j];

                    var left = row.Left;
                    var right = row.Right;

                    //Compute the amount of size needed to store this cell's text + a small amount of padding to the right of it
                    left.Property.MeasureContent(hdc);
                    left.Value.MeasureContent(hdc);

                    //Keep track of the longest cell of a given column
                    maxColumnWidths.Property1 = Math.Max(maxColumnWidths.Property1, left.Property.ContentWidth);
                    maxColumnWidths.Value1 = Math.Max(maxColumnWidths.Value1, left.Value.ContentWidth);

                    if (right != null)
                    {
                        var rightValue = right.Value;

                        rightValue.Property.MeasureContent(hdc);
                        rightValue.Value.MeasureContent(hdc);

                        maxColumnWidths.Property2 = Math.Max(maxColumnWidths.Property2, rightValue.Property.ContentWidth);
                        maxColumnWidths.Value2 = Math.Max(maxColumnWidths.Value2, rightValue.Value.ContentWidth);

                        row.Right = rightValue;
                    }
                }
            }

            _scrollHeight = (groups.Count * (_lineHeight + HEADER_BOTTOM_GAP + 1)) + (numRows * (_lineHeight + 1)) + (2 * _yPad);

            _maxColumnWidths = maxColumnWidths;

            _groups = groups.ToArray();

            ComputeLayout();

            Invalidate();

            User32.ReleaseDC(NativeHandle, hdc);
        }

        private void ProcessArchitectureVersion(PEFileOverview overview, ref ValueList<OverviewGroup> groups)
        {
            var items = new ValueList<OverviewEntry>();

            try
            {
                items.Add(new("Is64Bit", Bool(overview.Is64Bit), "IMAGE_OPTIONAL_HEADER.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC", forceEnabled: true));
                items.Add(new("Machine", overview.Machine.ToString(), "IMAGE_FILE_MACHINE.Machine"));
                items.Add(new("Subsystem", overview.Subsystem.ToString(), "IMAGE_OPTIONAL_HEADER.Subsystem"));
                items.Add(new("CorPlatform", overview.CorPlatform?.ToString(), "IMAGE_COR20_HEADER.Flags / IMAGE_OPTIONAL_HEADER.Magic"));
                //items.Add(("Company", overview.Company));
                items.Add(new("ImageVersion", overview.ImageVersion?.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]ImageVersion"));
                items.Add(new("LinkerVersion", overview.LinkerVersion.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]LinkerVersion"));
                items.Add(new("RichLinkerVersion", overview.RichLinkerVersion?.ToString(), overview.RichLinkerVersion != null ? $"Rich Header PRODITEM {overview.RichLinkerProdID}" : "Rich Header"));
                items.Add(new("SubsystemVersion", overview.SubsystemVersion.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]SubsystemVersion"));
                items.Add(new("OSVersion", overview.OSVersion.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]OperatingSystemVersion"));
                //items.Add(("FileVersion", overview.FileVersion.ToString()));
                //items.Add(("ProductVersion", overview.ProductVersion.ToString()));

                CreateGroup("Architecture / Version", ref items, ref groups);
            }
            finally
            {
                items.Dispose();
            }
        }

        private void ProcessEntryPointRichHeader(PEFileOverview overview, ref ValueList<OverviewGroup> group)
        {
            var items = new ValueList<OverviewEntry>();

            try
            {
                items.Add(new("EntryPoint", overview.EntryPoint?.ToString(), "IMAGE_OPTIONAL_HEADER.AddressOfEntryPoint"));
                items.Add(new("Cor20ManagedEntryPoint", overview.Cor20ManagedEntryPoint?.ToString(), "IMAGE_COR20_HEADER.EntryPointTokenOrRVA / IMAGE_COR20_HEADER.Flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT"));
                items.Add(new("Cor20NativeEntryPoint", overview.Cor20NativeEntryPoint?.ToString(), "IMAGE_COR20_HEADER.EntryPointTokenOrRVA / (IMAGE_COR20_HEADER.Flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT) == false"));
                items.Add(new("RichCompilerBackend", Join(overview.RichCompilerBackend), "Rich Header"));
                items.Add(new("RichLanguage", Join(overview.RichLanguage), "Rich Header"));
                items.Add(new("RichToolsetVersion", Join(overview.RichToolsetVersion), "Rich Header"));

                CreateGroup("EntryPoint / RichHeader", ref items, ref group);
            }
            finally
            {
                items.Dispose();
            }
        }

        private void ProcessManaged(PEFileOverview overview, ref ValueList<OverviewGroup> groups)
        {
            var items = new ValueList<OverviewEntry>();

            try
            {
                items.Add(new("IsManaged", Bool(overview.IsManaged), "IMAGE_COR20_HEADER"));
                items.Add(new("TargetFrameworkAttribute", overview.TargetFrameworkAttribute, "System.Runtime.Versioning.TargetFrameworkAttribute"));
                items.Add(new("DebuggableAttribute", overview.DebuggableAttribute?.ToString(), "System.Diagnostics.DebuggableAttribute"));
                items.Add(new("Cor20HeaderVersion", overview.Cor20HeaderVersion?.ToString(), "IMAGE_COR20_HEADER.[Major/Minor]RuntimeVersion"));
                items.Add(new("IsNGEN", Bool(overview.IsNgen), "IMAGE_COR20_HEADER.ManagedNativeHeader == NGE"));
                items.Add(new("NGEN Version", overview.NgenVersion?.ToString(), "CORCOMPILE_HEADER.[Major/Minor]Version"));
                items.Add(new("IsNativeAOT", Bool(overview.IsNativeAOT), "Export DotNetRuntimeDebugHeader"));
                items.Add(new("NativeAOTHeaderVersion", overview.NativeAOTHeaderVersion?.ToString(), "DotNetRuntimeDebugHeader.[Major/Minor]Version"));
                items.Add(new("IsAppHost", Bool(overview.IsAppHost), "AppHost Signature"));
                items.Add(new("IsSingleFileApp", Bool(overview.IsSingleFileApp), "Export DotNetRuntimeInfo"));
                items.Add(new("IsR2R", Bool(overview.IsR2R), "IMAGE_COR20_HEADER.ManagedNativeHeader == R2R"));
                items.Add(new("R2R HeaderVersion", overview.R2RHeaderVersion?.ToString(), "READYTORUN_HEADER.[Major/Minor]Version"));

                CreateGroup("Managed", ref items, ref groups);
            }
            finally
            {
                items.Dispose();
            }
        }

        private void ProcessDebug(PEFileOverview overview, ref ValueList<OverviewGroup> groups)
        {
            var items = new ValueList<OverviewEntry>();

            try
            {
                items.Add(new("CodeViewSig", overview.CodeViewSig.ToString(), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_CODEVIEW)"));
                items.Add(new("PDBFile", Join(overview.PDBFile), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_CODEVIEW)"));
                items.Add(new("DBGFile", overview.DBGFile?.ToString(), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_MISC)"));
                items.Add(new("LocalSymbolFile", overview.LocalSymbolFile, null));
                items.Add(new("HasEmbeddedPortablePDB", Bool(overview.HasEmbeddedPortablePDB), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB)"));
                items.Add(new("HasEmbeddedCoffSymbols", Bool(overview.HasEmbeddedCoffSymbols), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_COFF)"));
                items.Add(new("HasEmbeddedCodeViewSymbols", Bool(overview.HasEmbeddedCodeViewSymbols), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_CODEVIEW)"));
                items.Add(new("IsReproducible", Bool(overview.IsReproducible), "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_REPRO)"));

                CreateGroup("Debug", ref items, ref groups);
            }
            finally
            {
                items.Dispose();
            }
        }

        #endregion
        #region PDBFile

        private void ProcessPDBFile(PDBFileOverview overview)
        {
            var groups = new ValueList<OverviewGroup>();

            try
            {
                ProcessVersion(overview, ref groups);

                MeasureGroups(ref groups);
            }
            finally
            {
                groups.Dispose();
            }
        }

        private void ProcessVersion(PDBFileOverview overview, ref ValueList<OverviewGroup> groups)
        {
            var items = new ValueList<OverviewEntry>();

            try
            {
                //None of this makes sense for V1
                if (overview.PDBKind == PDB.PDBFileKind.V1)
                    throw new NotImplementedException();

                items.Add(new("Kind", overview.PDBKind.ToString(), ""));
                items.Add(new("PDBImpv", overview.PDBImpv.ToString(), ""));
                items.Add(new("DBIImpv", overview.DBIImpv.ToString(), ""));
                items.Add(new("TPIImpv", overview.TPIImpv.ToString(), ""));
                items.Add(new("IPIImpv", overview.IPIImpv.ToString(), ""));

                items.Add(new("PageSize", overview.PageSize.ToString(), ""));
                items.Add(new("Age", overview.Age.ToString(), ""));
                items.Add(new("Guid", overview.Guid.ToString(), ""));
                items.Add(new("Signature", overview.Signature.ToString(), ""));
                items.Add(new("Features", Join(overview.Features), ""));

                items.Add(new("HasStrippedFlag", Bool(overview.HasStrippedFlag), ""));
                items.Add(new("IsStripped", Bool(overview.IsStripped), ""));
                items.Add(new("HasSourceLink", Bool(overview.HasSourceLink), ""));
                items.Add(new("HasSrcSrv", Bool(overview.HasSrcSrv), ""));

                CreateGroup("Overview", ref items, ref groups);
            }
            finally
            {
                items.Dispose();
            }
        }

        #endregion
        #endregion
        #region Layout / Paint

        protected override void OnLayout()
        {
            base.OnLayout();

            ComputeLayout();
        }

        private void ComputeLayout()
        {
            if (_maxColumnWidths == null)
                return;

            var maxColumnWidths = _maxColumnWidths.Value;

            //We wish to have two panes separated by a divider. Certain values may be long, so based on the size of the window, we may need to clip
            //these values. The width of our properties should always remain fixed. If the window is shrunk too much, don't allow the space to be shrunk
            //any further; display a horizontal scrollbar

            const int xPad = 8; //Each pane has this padding to the left and right of it (which means there's 4x padding between both panes)
            var availableWidthForPropertiesAndValues = ClientSize.Width - (xPad * 4) - DIVIDER_GAP;

            var availableWidthForValues = availableWidthForPropertiesAndValues - (maxColumnWidths.Property1 + maxColumnWidths.Property2);

            var valueWidthPerPane = availableWidthForValues / 2;

            const int minValueWidth = 30;

            if (valueWidthPerPane < minValueWidth)
                valueWidthPerPane = minValueWidth;

            var xPos = xPad - _xPad; //There's already a client edge border which is 2px, so we don't need to start at 8 we need to start at 6

            var property1Width = maxColumnWidths.Property1;

            var pane1NeededWidth = property1Width + maxColumnWidths.Property2 + 15;
            var effectivePaneWidth = Math.Min(pane1NeededWidth, valueWidthPerPane + property1Width);

            _columnBounds.Property1 = new Bound(xPos, property1Width);
            _columnBounds.Value1 = new Bound(xPos + property1Width, effectivePaneWidth - (xPos + property1Width) + DIVIDER_GAP);

            xPos += effectivePaneWidth + xPad + DIVIDER_GAP;
            _dividerXPos = xPos - (DIVIDER_GAP / 2);

            xPos += xPad;

            var property2Width = maxColumnWidths.Property2;

            var remainingSpace = ClientSize.Width - (xPos + xPad);

            //Value1 sized itself to just take up what it needs; allocate all of the remaining space to Value2
            var availableWidthForValue = (ClientSize.Width - (xPos + property2Width)) - (xPad / 2);

            //var effectiveValue2Width = Math.Min(availableWidthForValue, valueWidthPerPane);
            var effectiveValue2Width = availableWidthForValue;

            //If the property goes off the screen, we need to clamp it
            if (property2Width > remainingSpace)
            {
                property2Width = Math.Max(0, remainingSpace);
                effectiveValue2Width = 0;
            }

            remainingSpace -= property2Width;

            _columnBounds.Property2 = new Bound(xPos, property2Width);
            _columnBounds.Value2 = new Bound(xPos + property2Width, maxColumnWidths.Value2);

            _scrollWidth = xPos + property2Width + maxColumnWidths.Value2;

            Invalidate();
        }

        protected unsafe override void OnPaint(HDC hdc)
        {
            User32.FillRect(hdc, ClientRectangle, Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH));

            if (_groups == null)
                return;

            Gdi32.SelectObject(hdc, _font.hFont);

            var xPos = -_scrollXPos;
            var yPos = -_scrollYPos;

            RECT rect;

            Gdi32.SetBkMode(hdc, BACKGROUND_MODE.TRANSPARENT);

            var columnBounds = _columnBounds;

            const int headerXPad = 10;

            for (var i = 0; i < _groups.Length; i++)
            {
                var isSelectedGroup = _selectedItem.group == i;

                rect.left = _xPad + headerXPad; //Extra xPad here
                rect.top = yPos;
                rect.bottom = yPos + _lineHeight;

                ref var group = ref _groups[i];

                Gdi32.GetTextExtentPoint32W(hdc, group.Name, out var size);
                rect.right = rect.left + size.cx;

                User32.OffsetRect(ref rect, xPos, 0);

                fixed (char* c = group.Name)
                {
                    //UxTheme.DrawThemeTextEx(_hTheme, hdc, (int) LISTVIEWPARTS.LVP_GROUPHEADER, 0, c, group.Name.Length, DT_CENTER | DT_SINGLELINE | DT_CALCRECT | DT_NOPREFIX | DT_EDITCONTROL, &rect, default);

                    DTTOPTS opts = new DTTOPTS();
                    opts.dwSize = sizeof(DTTOPTS);
                    opts.dwFlags = DTT_FONTPROP | DTT_COLORPROP | DTT_CALCRECT;
                    opts.iFontPropId = (int) THEME_PROPERTY_SYMBOL_ID.TMT_FONT;
                    opts.iColorPropId = (int) THEME_PROPERTY_SYMBOL_ID.TMT_HEADING1TEXTCOLOR;

                    UxTheme.DrawThemeTextEx(_hTheme, hdc, (int) LISTVIEWPARTS.LVP_GROUPHEADER, 0, c, group.Name.Length, DT_VCENTER | DT_WORDBREAK | DT_SINGLELINE | DT_NOCLIP | DT_NOPREFIX | DT_EDITCONTROL | DT_END_ELLIPSIS, &rect, &opts);

                    rect.left = rect.right + 3; //text + 114?
                    rect.top = 12 + yPos; //text+10?
                    rect.right = Width - _xPad - headerXPad;
                    rect.bottom = 13 + yPos; //text-9?
                    UxTheme.DrawThemeBackground(_hTheme, hdc, (int) LISTVIEWPARTS.LVP_GROUPHEADERLINE, (int) GROUPHEADERLINESTATES.LVGHL_OPEN, &rect);
                }

                yPos += _lineHeight + HEADER_BOTTOM_GAP;

                for (var j = 0; j < group.Rows.Length; j++)
                {
                    var row = group.Rows[j];

                    var isSelectedRow = isSelectedGroup && _selectedItem.row == j;

                    PaintEntry(hdc, row.Left, columnBounds.Property1, columnBounds.Value1, isSelectedRow && _selectedItem.column == 0, xPos, yPos);

                    var dividerRect = new RECT(_dividerXPos, yPos - 2, _dividerXPos + 1, yPos + _lineHeight - 1);
                    User32.OffsetRect(ref dividerRect, xPos, 0);
                    UxTheme.DrawThemeBackground(_hTheme, hdc, (int) LISTVIEWPARTS.LVP_GROUPHEADERLINE, (int) GROUPHEADERLINESTATES.LVGHL_OPEN, dividerRect, default);

                    if (row.Right != null)
                    {
                        PaintEntry(hdc, row.Right.Value, columnBounds.Property2, columnBounds.Value2, isSelectedRow && _selectedItem.column == 1, xPos, yPos);
                    }

                    yPos += _lineHeight + 1;
                }

                yPos++;
            }

            UpdateScrollBar();
        }

        private void PaintEntry(HDC hdc, OverviewEntry entry, Bound propertyBound, Bound valueBound, bool isSelected, int xPos, int yPos)
        {
            RECT rect = new RECT();
            rect.top = yPos;
            rect.bottom = yPos + _lineHeight;
            rect.left = propertyBound.Left;
            rect.right = propertyBound.Right;

            User32.OffsetRect(ref rect, xPos, 0);

            //Show "false" in black for Is64Bit
            var isDisabled = !entry.ForceEnabled && !HasActiveValue(entry.Value.Value);

            COLORREF oldFg = default;

            var restoreText = false;

            if (isSelected)
            {
                //Right clicking an item is not enough to give the window focus; it seems we have to call SetFocus as well
                var isFocused = Focused;

                var highlight = User32.GetSysColor(isFocused ? SYS_COLOR_INDEX.COLOR_HIGHLIGHT : SYS_COLOR_INDEX.COLOR_3DFACE);
                var hBrush = Gdi32.CreateSolidBrush(new COLORREF(highlight)); //todo: cache

                var bgRect = rect;
                bgRect.left -= 3;
                bgRect.right = valueBound.Right;

                User32.FillRect(hdc, bgRect, hBrush);

                if (isFocused)
                {
                    oldFg = Gdi32.SetTextColor(hdc, new COLORREF(User32.GetSysColor(SYS_COLOR_INDEX.COLOR_HIGHLIGHTTEXT)));
                    restoreText = true;
                }
            }
            else if (isDisabled)
            {
                oldFg = Gdi32.SetTextColor(hdc, _grayColor);
                restoreText = true;
            }

            User32.DrawTextW(hdc, entry.Property.Value!, ref rect, DT_WORD_ELLIPSIS | DT_VCENTER);

            rect.left = valueBound.Left;
            rect.right = valueBound.Right;

            User32.OffsetRect(ref rect, xPos, 0);

            if (entry.Value.Value != null)
                User32.DrawTextW(hdc, entry.Value.Value, ref rect, DT_WORD_ELLIPSIS | DT_VCENTER);

            if (restoreText)
            {
                Gdi32.SetTextColor(hdc, oldFg);
            }
        }

        #endregion
        #region WndProc

        protected unsafe override void OnHandleCreated()
        {
            _hTheme = UxTheme.OpenThemeData(NativeHandle, "ListView");

            _font = UIElement.DefaultFont;

            var hdc = User32.GetDCEx(NativeHandle, default, GET_DCX_FLAGS.DCX_CACHE);

            Gdi32.SelectObject(hdc, _font.hFont);
            Gdi32.GetTextMetricsW(hdc, out var tm);
            _lineHeight = tm.tmHeight + 4; //2x padding above and below

            User32.ReleaseDC(NativeHandle, hdc);

            _xPad = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXEDGE);
            _yPad = User32.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYEDGE);
        }

        protected override void OnHandleDestroyed()
        {
            if (_hTheme != default)
            {
                UxTheme.CloseThemeData(_hTheme);
                _hTheme = default;
            }

            //Don't dispose font; it doesn't belong to us
        }

        protected override void OnMouseDown(int x, int y)
        {
            base.OnMouseDown(x, y);

            var target = HitTest(x, y);

            var isLeftMouseDown = User32.GetKeyState((int) VIRTUAL_KEY.VK_LBUTTON) < 0;
            var isRightMouseDown = User32.GetKeyState((int) VIRTUAL_KEY.VK_RBUTTON) < 0;

            if (target != _selectedItem)
            {
                _selectedItem = target;
                Invalidate();
            }
            
            if (isRightMouseDown && target.columnIndex != -1)
            {
                var row = _groups![target.groupIndex].Rows[target.rowIndex];
                var entry = target.columnIndex == 0 ? row.Left : row.Right;

                var hMenu = User32.CreatePopupMenu();
                User32.AppendMenuW(hMenu, MENU_ITEM_FLAGS.MF_STRING, 1000, $"Copy \"{entry!.Value.Value.Value}\"\tCtrl+C");
                User32.AppendMenuW(hMenu, MENU_ITEM_FLAGS.MF_STRING, 1002, "Open File Location");
                User32.AppendMenuW(hMenu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, (PCWSTR) default);
                User32.AppendMenuW(hMenu, MENU_ITEM_FLAGS.MF_STRING, 1001, $"Go to {entry.Value.Source}");

                //TrackPopupMenu wants the cursor position in screen coordinates
                User32.GetCursorPos(out var pt);

                User32.TrackPopupMenu(hMenu, TRACK_POPUP_MENU_FLAGS.TPM_TOPALIGN | TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN, pt.x, pt.y, NativeHandle, default);
            }
        }

        private (int groupIndex, int rowIndex, int columnIndex) HitTest(int x, int y)
        {
            var groups = _groups;

            x += _scrollXPos;
            y += _scrollYPos;

            var yPos = _yPad;

            for (var i = 0; i < groups!.Length; i++)
            {
                ref var group = ref groups[i];

                var bottomY = yPos + _lineHeight + HEADER_BOTTOM_GAP;

                if (y > yPos && y <= bottomY)
                {
                    //They clicked inside the header
                    return (i, -1, -1);
                }

                yPos = bottomY;

                for (var j = 0; j < group.Rows.Length; j++)
                {
                    ref var row = ref group.Rows[j];

                    bottomY = yPos + _lineHeight;

                    if (y >= yPos && y <= bottomY) //>= to include the 1 pixel before us in our bounds
                    {
                        var columnBounds = _columnBounds;

                        //They clicked in this row. Which column did they click on?

                        if (x >= columnBounds.Property1.Left && x < columnBounds.Value1.Right)
                        {
                            return (i, j, 0);
                        }

                        if (row.Right != null && x >= columnBounds.Property2.Left && x < columnBounds.Value2.Right)
                        {
                            return (i, j, 1);
                        }

                        //Clicked outside the bounds of any columns
                        return (i, j, -1);
                    }

                    yPos = bottomY + 1;
                }

                yPos++;
            }

            return (-1, -1, -1);
        }

        protected override void OnKillFocus()
        {
            if (_selectedItem.column != -1)
                Invalidate(); //Need to redraw it as not focused

            base.OnKillFocus();
        }

        #endregion
        #region Scroll

        protected override void OnMouseWheel(int delta)
        {
            //Mouse wheel moved 72, which is equal to 3x24 = line height (20) + 2 padding above and below

            var distance = 3 * _lineHeight;

            //< 0 = scrolling down; content should be moved up
            //> 0 = scrolling up; content should be moved down
            if (delta < 0)
                distance = -distance;

            TryScrollY(distance);
        }

        protected override void OnVScroll(SB sb, int trackPos)
        {
            switch (sb)
            {
                case SB.SB_THUMBPOSITION:
                    break;

                case SB.SB_THUMBTRACK:
                    //We're scrolling to absolute positions here

                    if (trackPos != _scrollYPos)
                    {
                        _scrollYPos = trackPos;
                        Invalidate();
                    }

                    break;

                case SB.SB_LINEUP:
                    TryScrollY(_lineHeight);
                    break;

                case SB.SB_LINEDOWN:
                    TryScrollY(-_lineHeight);
                    break;

                case SB.SB_PAGEUP:
                    TryScrollY(ClientSize.Height);
                    break;

                case SB.SB_PAGEDOWN:
                    TryScrollY(-ClientSize.Height);
                    break;

                case SB.SB_TOP:
                case SB.SB_BOTTOM:
                    throw new NotImplementedException();
            }
        }

        protected override void OnHScroll(SB sb, int trackPos)
        {
            switch (sb)
            {
                case SB.SB_THUMBTRACK:
                    //We're scrolling to absolute positions here

                    if (trackPos != _scrollXPos)
                    {
                        _scrollXPos = trackPos;
                        Invalidate();
                    }

                    break;

                case SB.SB_LINEUP:
                    TryScrollX(8);
                    break;

                case SB.SB_LINEDOWN:
                    TryScrollX(-8);
                    break;

                case SB.SB_PAGEUP:
                    TryScrollX(ClientSize.Width);
                    break;

                case SB.SB_PAGEDOWN:
                    TryScrollX(-ClientSize.Width);
                    break;
            }
        }

        private void TryScrollY(int distance)
        {
            var newScrollPos = _scrollYPos - distance;

            if (distance < 0)
            {
                //Scrolling down

                var maxHeight = Math.Min(_scrollHeight, ClientHeight);

                var limit = _scrollHeight - maxHeight;

                if (newScrollPos > limit)
                    newScrollPos = limit;
            }
            else
            {
                //Scrolling up

                if (newScrollPos < 0)
                    newScrollPos = 0;
            }

            if (newScrollPos != _scrollYPos)
            {
                _scrollYPos = newScrollPos;
                Invalidate();
            }
        }

        private void TryScrollX(int distance)
        {
            var newScrollPos = _scrollXPos - distance;

            if (distance < 0)
            {
                var limit = _scrollWidth - ClientSize.Width;

                if (newScrollPos > limit)
                    newScrollPos = limit;
            }
            else
            {
                if (newScrollPos < 0)
                    newScrollPos = 0;
            }

            if (newScrollPos != _scrollXPos)
            {
                _scrollXPos = newScrollPos;
                Invalidate();
            }
        }

        private unsafe void UpdateScrollBar()
        {
            if (_scrollHeight <  ClientHeight)
            {
                EnableVerticalScroll = false;
                return;
            }

            EnableVerticalScroll = true;
            SetScrollRange(true, min: 0, max: _scrollHeight, pageSize: ClientHeight + (_xPad * 2), pos: _scrollYPos);
        }

        #endregion
        #region Format Helpers

        private void CreateGroup(string name, ref ValueList<OverviewEntry> items, ref ValueList<OverviewGroup> groups)
        {
            var rowsPerColumn = GetRowsPerColumn(items.Count, 2);

            //The first column will always have the most (or equal to the most) rows
            var rows = new OverviewRow[rowsPerColumn[0]];

            //Tracks how many items we've eaten from the pooled list
            var nextItemIndex = 0;

            //For each column
            for (var i = 0; i < rowsPerColumn.Length; i++) //Implicitly, since we only support two columns, this will only loop twice
            {
                /* The UI will have two halves, separated by a divider. Each half then has two columns
                 * inside of it: a property name and a property value, giving 4 columns total. We construct our
                 * layout _column first_, meaning we assign items to the left column until we run out of rows, at which
                 * point we start filling in the right column */

                var numRows = rowsPerColumn[i];

                if (i == 0)
                {
                    //It's the first column, which will have the maximum number of rows in it

                    //Construct each of the rows
                    for (var j = 0; j < numRows; j++)
                    {
                        var row = new OverviewRow
                        {
                            Left = items[nextItemIndex++]
                        };

                        rows[j] = row;
                    }
                }
                else
                {
                    //We're now processing the second column. All of the rows we'll need were already initialized when we were processing
                    //the first column, so now we just need to continue eating items from the pooled list and assigning them to the second
                    //column until we run out

                    for (var j = 0; j < numRows; j++)
                    {
                        ref var row = ref rows[j];

                        row.Right = items[nextItemIndex++];
                    }
                }
            }

            groups.Add(new OverviewGroup(name, rows));
        }

        private int[] GetRowsPerColumn(int numItems, int numColumns)
        {
            var approxRowsPerColumn = numItems / numColumns;
            var remainder = numItems % numColumns;

            var results = new int[numColumns];

            for (var i = 0; i < numColumns; i++)
                results[i] = approxRowsPerColumn + (i < remainder ? 1 : 0);

            return results;
        }

        private string? Join<T>(T[]? value)
        {
            if (value == null)
                return null;

            Debug.Assert(value.Length > 0);

            return string.Join(", ", value);
        }

        private string Bool(bool value) => value ? "true" : "false";

        private static bool HasActiveValue(string? value)
        {
            return value != null && value != "false";
        }

        #endregion
    }
}
