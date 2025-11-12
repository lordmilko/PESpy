using System;
using System.Diagnostics;
using System.Drawing;
#if WINFORMS
using System.Windows.Forms;
#endif
using PInvoke;
#if WINFORMS
using ListViewSubItem = System.Windows.Forms.ListViewItem.ListViewSubItem;
#else
using ListView = PESpy.Controls.NativeListView;
using ListViewGroup = PESpy.Controls.NativeListViewGroup;
using ListViewItem = PESpy.Controls.NativeListViewItem;
using ListViewSubItem = PESpy.Controls.NativeListViewItem.NativeListViewSubItem;
using ToolTip = PESpy.Controls.NativeTooltip;
#endif

namespace PESpy.Controls
{
    public class OverviewPanel : ListView
    {
        private HTHEME _hTheme;
        private ToolTip _tooltip;
        private int _lastToolTipPos = -1;

        private static readonly Color _grayColor = Color.FromArgb(0x8a8a8a);

        private const int DIVIDER_GAP = 10;

#if WINFORMS
        public HWND hWnd => Handle;
#endif

        public OverviewPanel()
        {
#if WINFORMS
            DoubleBuffered = true;

            View = System.Windows.Forms.View.Details;
            HeaderStyle = ColumnHeaderStyle.None;
#endif

            App.AnalysisCompleted += (s, e) =>
            {
                BeginInvoke(() =>
                {
                    ProcessPEFile((PEFileOverview) App.FileAccessor.Overview);
                });
            };
        }

#if !WINFORMS
        private void BeginInvoke(Action action) => throw new NotImplementedException();
#endif

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _hTheme = UxTheme.OpenThemeData(hWnd, "ListView");
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            base.OnHandleDestroyed(e);

            if (_hTheme != default)
            {
                UxTheme.CloseThemeData(_hTheme);
                _hTheme = default;
            }
        }

        protected unsafe override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
        {
            var needGray = true;

            if (e.ColumnIndex % 2 == 0)
            {
                //We're drawing the header. If this is an active value, we can just
                //draw default. Otherwise, we should draw the text in a lighter color

                if (e.SubItem.Name != "Is64Bit" && IsActiveValue(e.Item.SubItems[e.ColumnIndex + 1]))
                {
                    needGray = false;
                }
            }
            else
            {
                /* We're drawing the value. If the value doesn't have a value,
                 * there's nothing to do. If the value does have a value, we can draw
                 * default _unless_ our associated column stores a boolean value, in which
                 * case we should draw it as disabled unless it's a value we always want to show
                 * as black */

                //Not sure if we need to white out the region, so draw default even if the value is null
                if (e.SubItem!.Text == string.Empty || IsActiveValue(e.SubItem))
                {
                    needGray = false;
                }
            }

#if WINFORMS
            var hdc = e.Graphics.GetHdc();
#else
            HDC hdc = default;
            throw new NotImplementedException();
#endif

            bool needDivider = e.ColumnIndex % 2 == 1 && e.ColumnIndex != Columns.Count - 1;

            var textBounds = e.Bounds;

            if (needDivider)
                textBounds.Width -= DIVIDER_GAP;

            TreeListView.DrawString(hdc, needGray ? _grayColor : e.Item.ForeColor, e.SubItem.Text, default, textBounds, 0, out _);

            var bounds = e.Bounds;

            if (needDivider)
            {
                var left = bounds.Right - (DIVIDER_GAP / 2);

                var rect = new RECT(left, bounds.Top, left + 1, bounds.Bottom + 1);

                UxTheme.DrawThemeBackground(_hTheme, hdc, (int) LISTVIEWPARTS.LVP_GROUPHEADERLINE, (int) GROUPHEADERLINESTATES.LVGHL_OPEN, rect, default);
            }

#if WINFORMS
            e.Graphics.ReleaseHdc(hdc);
#endif
        }

        private bool IsActiveValue(ListViewSubItem subItem)
        {
            if (subItem.Text == string.Empty || subItem.Text == "false")
                return false;

            return true;
        }

        #region PEFile

        private unsafe void ProcessPEFile(PEFileOverview overview)
        {
            var maxColumns = 0;

            ProcessArchitectureVersion(overview, ref maxColumns);
            ProcessEntryPointRichHeader(overview, ref maxColumns);
            ProcessManaged(overview, ref maxColumns);
            ProcessDebug(overview, ref maxColumns);

            for (var i = 0; i < maxColumns; i++)
            {
                //We need to add a "Property" and "Value" column. These columns will be hidden
                Columns.Add("Property");
                Columns.Add("Value");
            }

            var hdc = User32.GetDCEx(hWnd, default, GET_DCX_FLAGS.DCX_CACHE);

            var hFont = Font.ToHfont();

            Gdi32.SelectObject(hdc, hFont);

            //Now measure the required size of each column
            var columnWidths = new int[maxColumns * 2];

            const int padding = 15;

            for (var i = 0; i < Items.Count; i++)
            {
                var subItems = Items[i].SubItems;

                for (var j = 0; j < subItems.Count; j++)
                {
                    var subItem = subItems[j];

                    fixed (char* c = subItem.Text)
                    {
                        SIZE size;
                        Gdi32.GetTextExtentPoint32W(hdc, c, subItem.Text.Length, &size);

                        var effectiveWidth = size.cx + padding;

                        //If this is not the last pair of columns, add some extra padding for the divider
                        if (j % 2 == 1 && j / 2 < maxColumns - 1)
                            effectiveWidth += DIVIDER_GAP;

                        columnWidths[j] = Math.Max(columnWidths[j], effectiveWidth);
                    }
                }
            }

            //If there's a big string in the left hand pane, prevent that from pushing the right
            //hand pane over too far

            var numPanes = columnWidths.Length / 2;

            var widthPerPane = ClientSize.Width / numPanes;

            for (var i = 0; i < columnWidths.Length; i += 2)
            {
                var paneWidth = columnWidths[i] + columnWidths[i + 1];

                if (paneWidth > widthPerPane)
                    columnWidths[i + 1] = widthPerPane - columnWidths[i];
            }

            for (var i = 0; i < columnWidths.Length; i++)
                Columns[i].Width = columnWidths[i];

            Gdi32.DeleteObject(hFont);

            User32.ReleaseDC(hWnd, hdc);
        }

        private void ProcessArchitectureVersion(PEFileOverview overview, ref int maxColumns)
        {
            var items = new PooledList<(string key, string? value, string? source)>();

            try
            {
                items.Add(("Is64Bit", Bool(overview.Is64Bit), "IMAGE_OPTIONAL_HEADER.Magic == IMAGE_NT_OPTIONAL_HDR64_MAGIC"));
                items.Add(("Machine", overview.Machine.ToString(), "IMAGE_FILE_MACHINE.Machine"));
                items.Add(("Subsystem", overview.Subsystem.ToString(), "IMAGE_OPTIONAL_HEADER.Subsystem"));
                items.Add(("CorPlatform", overview.CorPlatform?.ToString(), "IMAGE_COR20_HEADER.Flags / IMAGE_OPTIONAL_HEADER.Magic"));
                items.Add(("ImageVersion", overview.ImageVersion?.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]ImageVersion"));
                items.Add(("LinkerVersion", overview.LinkerVersion.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]LinkerVersion"));
                items.Add(("RichLinkerVersion", overview.RichLinkerVersion?.ToString(), overview.RichLinkerVersion != null ? $"Rich Header PRODITEM {overview.RichLinkerProdID}" : "Rich Header"));
                items.Add(("SubsystemVersion", overview.SubsystemVersion.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]SubsystemVersion"));
                items.Add(("OSVersion", overview.OSVersion.ToString(), "IMAGE_OPTIONAL_HEADER.[Major/Minor]OperatingSystemVersion"));
                CreateGroup("Architecture / Version", ref items, ref maxColumns);
            }
            finally
            {
                items.Dispose();
            }
        }

        private void ProcessEntryPointRichHeader(PEFileOverview overview, ref int maxColumns)
        {
            var items = new PooledList<(string key, string? value, string? source)>();

            try
            {
                items.Add(("EntryPoint",             overview.EntryPoint?.ToString(),             "IMAGE_OPTIONAL_HEADER.AddressOfEntryPoint"));
                items.Add(("Cor20ManagedEntryPoint", overview.Cor20ManagedEntryPoint?.ToString(), "IMAGE_COR20_HEADER.EntryPointTokenOrRVA / IMAGE_COR20_HEADER.Flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT"));
                items.Add(("Cor20NativeEntryPoint",  overview.Cor20NativeEntryPoint?.ToString(),  "IMAGE_COR20_HEADER.EntryPointTokenOrRVA / (IMAGE_COR20_HEADER.Flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT) == false"));
                items.Add(("RichCompilerBackend",    Join(overview.RichCompilerBackend),          "Rich Header"));
                items.Add(("RichLanguage",           Join(overview.RichLanguage),                 "Rich Header"));
                items.Add(("RichToolsetVersion",    Join(overview.RichToolsetVersion),            "Rich Header"));

                CreateGroup("EntryPoint / RichHeader", ref items, ref maxColumns);
            }
            finally
            {
                items.Dispose();
            }
        }

        private void ProcessManaged(PEFileOverview overview, ref int maxColumns)
        {
            var items = new PooledList<(string key, string? value, string? source)>();

            try
            {
                items.Add(("IsManaged",                Bool(overview.IsManaged),                 "IMAGE_COR20_HEADER"));
                items.Add(("TargetFrameworkAttribute", overview.TargetFrameworkAttribute,        "System.Runtime.Versioning.TargetFrameworkAttribute"));
                items.Add(("DebuggableAttribute",      overview.DebuggableAttribute?.ToString(), "System.Diagnostics.DebuggableAttribute"));
                items.Add(("Cor20HeaderVersion",       overview.Cor20HeaderVersion?.ToString(),  "IMAGE_COR20_HEADER.[Major/Minor]RuntimeVersion"));
                items.Add(("IsNGEN",                   Bool(overview.IsNgen),                    "IMAGE_COR20_HEADER.ManagedNativeHeader == NGE"));
                items.Add(("NGEN Version",             overview.NgenVersion?.ToString(),         "CORCOMPILE_HEADER.[Major/Minor]Version"));
                items.Add(("IsNativeAOT",              Bool(overview.IsNativeAOT),               "Export DotNetRuntimeDebugHeader"));
                items.Add(("NativeAOTHeaderVersion",   overview.NativeAOTHeaderVersion?.ToString(), "DotNetRuntimeDebugHeader.[Major/Minor]Version"));
                items.Add(("IsAppHost",                Bool(overview.IsAppHost),                  "AppHost Signature"));
                items.Add(("IsSingleFileApp",          Bool(overview.IsSingleFileApp),            "Export DotNetRuntimeInfo"));
                items.Add(("IsR2R",                    Bool(overview.IsR2R),                      "IMAGE_COR20_HEADER.ManagedNativeHeader == R2R"));
                items.Add(("R2R HeaderVersion",        overview.R2RHeaderVersion?.ToString(),     "READYTORUN_HEADER.[Major/Minor]Version"));

                CreateGroup("Managed", ref items, ref maxColumns);
            }
            finally
            {
                items.Dispose();
            }
        }

        private void ProcessDebug(PEFileOverview overview, ref int maxColumns)
        {
            var items = new PooledList<(string key, string? value, string? source)>();

            try
            {
                items.Add(("CodeViewSig",                overview.CodeViewSig.ToString(),                "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_CODEVIEW)"));
                items.Add(("PDBFile",                    Join(overview.PDBFile),                         "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_CODEVIEW)"));
                items.Add(("DBGFile",                    overview.DBGFile.ToString(),                    "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_MISC)"));
                items.Add(("LocalSymbolFile",            overview.LocalSymbolFile,                       null));
                items.Add(("HasEmbeddedPortablePDB",     Bool(overview.HasEmbeddedPortablePDB),          "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB)"));
                items.Add(("HasEmbeddedCoffSymbols",     Bool(overview.HasEmbeddedCoffSymbols),          "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_COFF)"));
                items.Add(("HasEmbeddedCodeViewSymbols", Bool(overview.HasEmbeddedCodeViewSymbols),      "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_CODEVIEW)"));
                items.Add(("IsReproducible",             Bool(overview.IsReproducible),                  "IMAGE_DEBUG_DIRECTORY (IMAGE_DEBUG_TYPE_REPRO)"));

                CreateGroup("Debug", ref items, ref maxColumns);
            }
            finally
            {
                items.Dispose();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var hitTest = HitTest(e.Location);

            if (hitTest.SubItem == null)
            {
                if (_lastToolTipPos != -1)
                {
                    _tooltip.SetToolTip(this, null);
                    _lastToolTipPos = -1;
                }
            }
            else
            {
                if (_lastToolTipPos == e.Location.X)
                    return; //No change

                //We're hovering over an item, but are we actually hovering over its text?

                var headerSubItem = hitTest.SubItem;

                var index = hitTest.Item.SubItems.IndexOf(headerSubItem);

                if (index % 2 == 1)
                {
                    //They're hovering over the value; the tooltip text is on the header
                    headerSubItem = hitTest.Item.SubItems[index - 1];
                }

                if (headerSubItem.Tag == null)
                    return;

                var str = headerSubItem.Tag.ToString();

                //Note: this isn't currently working; also if the window wasn't big enough
                //to show everything, we need to have the on hover show the clipped text

                if (_lastToolTip != str)
                {
                    _tooltip.SetToolTip(this, null);
                    _tooltip.SetToolTip(this, str);
                    _lastToolTipPos = e.Location.X;
                    _lastToolTip = str;
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_lastToolTipPos != -1)
            {
                _lastToolTipPos = -1;
                _lastToolTip = null;
                _tooltip.SetToolTip(this, null);
            }

            base.OnMouseLeave(e);
        }

        private string? Join<T>(T[]? value)
        {
            if (value == null)
                return null;

            return string.Join(", ", value);
        }

        private string Bool(bool value) => value ? "true" : "false";

        private void CreateGroup(string name, ref PooledList<(string key, string? value, string? source)> items, ref int maxColumns)
        {
            /* The key challenge we have is determining how many columns we should show. At most
             * we can show 3 columns. Conceptually, the wrapping should occur as follows
             * 
             * 1 item = 1
             * 2 items = 2
             * 3 items = 3
             * 4 items = 4
             * 5 items = 3/2
             * 6 items = 3/3
             * 7 items = 4/3
             * 8 items = 4/4
             * 9 items = 3/3/3
             * 10 items = 4/4/2
             * 11 items = 4/4/3
             * 12 items = 4/4/4
             * 13 items = 5/4/4
             * 14 items = 5/5/4
             * 15 items = 5/5/5
             * 16 items = 6/5/5
             * 
             * Essentially, there are five separate wrapping schemes here
             * - 1-4
             * - 5-8
             * - 9
             * - 10 (the default distribution would give 4/3/3)
             * - 11+
             */

            var group = new ListViewGroup(name);

            int[] rowsPerColumn;

            if (items.Count <= 4)
                rowsPerColumn = GetRowsPerColumn(items.Count, 1);
            else if (items.Count <= 8)
                rowsPerColumn = GetRowsPerColumn(items.Count, 2);
            else if (items.Count == 10)
                rowsPerColumn = new[] { 4, 4, 2 };
            else
                rowsPerColumn = GetRowsPerColumn(items.Count, 2); //9 and >10

            //The first column will always have the most (or equal to the most) rows
            var results = new ListViewItem[rowsPerColumn[0]];

            maxColumns = Math.Max(maxColumns, rowsPerColumn.Length);

            var nextItemIndex = 0;

            //For each column
            for (var i = 0; i < rowsPerColumn.Length; i++)
            {
                var numRows = rowsPerColumn[i];

                if (i == 0)
                {
                    //It's the first column, so we need to create list view items

                    for (var j = 0; j < numRows; j++)
                    {
                        var item = items[nextItemIndex++];

                        var lvi = new ListViewItem(item.key)
                        {
                            Group = group
                        };

                        lvi.SubItems[0].Tag = item.source;
                        lvi.SubItems.Add(item.value);

                        results[j] = lvi;
                    }
                }
                else
                {
                    //It's a subsequent column, so we need to create subitems

                    for (var j = 0; j < numRows; j++)
                    {
                        var item = items[nextItemIndex++];

                        var lvi = results[j];

                        lvi.SubItems.Add(item.key).Tag = item.source;
                        lvi.SubItems.Add(item.value);
                    }
                }
            }

            Items.AddRange(results);
            Groups.Add(group);
        }

        private int[] GetRowsPerColumn(int numItems, int numColumns)
        {
            var approxRowsPerColumn = numItems / numColumns;
            var remainder = numItems % numColumns;

            var results = new int[numColumns];

            for (var i = 0; i <  numColumns; i++)
                results[i] = approxRowsPerColumn + (i < remainder ? 1 : 0);

            return results;
        }
    }
}
