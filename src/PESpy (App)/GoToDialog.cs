using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using PESpy.View;
using PInvoke;

namespace PESpy
{
    public partial class GoToDialog : Form
    {
        class GoToResult
        {
            public FixedUtf8String Name;
            public FixedUtf16String NameWide;
            public int Offset;
            public string Section;
            public int RVA;
            public ulong VA;

            public RECT Rect;
            public Bitmap Image;

            public int Score;

            public List<Range>? NameMatches;
            public List<Range>? OffsetMatches;
            public List<Range>? RVAMatches;
            public List<Range>? VAMatches;

            public void ComputeScore(int queryLength)
            {
                var totalScore = 0;

                if (NameMatches != null)
                {
                    //Compute a score for each matched range

                    var nameScore = 0;

                    foreach (var range in NameMatches)
                    {
                        var score = 100;

                        var nameLength = Name.Length == 0 ? NameWide.Length : Name.Length;

                        //Prefer matches at the start
                        if (range.Start == 0)
                        {
                            if (range.Length == queryLength)
                                score *= 20; //Perfect match
                            else
                                score += 200; //Prefix
                        }

                        //Prefer matches that cover large ranges
                        nameScore = Math.Max(nameScore, score);
                    }

                    totalScore += nameScore;
                }

                if (OffsetMatches != null)
                {
                    var score = 50;
                    throw new NotImplementedException();
                }

                if (RVAMatches != null)
                {
                    var score = 50;

                    throw new NotImplementedException();
                }

                if (VAMatches != null)
                {
                    var score = 50;

                    throw new NotImplementedException();
                }

                Score = totalScore;
            }

            public struct Range
            {
                public int Start;
                public int Length;
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }
        private const int BORDER_WIDTH = 1;

        //The height of a single row in the results list
        private const int RESULT_ROW_HEIGHT = 26;
        private const int RESULT_PADDING = 1; //Shared gap between each result; also the gap between a result and the main window border
        private const int RESULT_COLUMN_GAP = 36; //Minimum number of pixels gap there should be between two columns
        private const int MIN_RESULTS_WIDTH = 515; //Minimum number of pixels the entire window should be set to for showing results
        private const int RESULT_ROW_START = BORDER_WIDTH + RESULT_PADDING;
        private const int RESULT_IMAGE_WIDTH = 35;
        private const int RESULT_TEXT_START = RESULT_ROW_START + RESULT_IMAGE_WIDTH;
        private const int RESULT_ROW_END = 8;
        private const int RESULT_HEADER_TOP = 1;
        private const int RESULT_HEADER_BOTTOM_GAP = 3;

        private const int MIN_NAME_WIDTH = 191;
        private const int MIN_OFFSET_WIDTH = 35;
        private const int MIN_RVA_WIDTH = 62;
        private const int MIN_VA_WIDTH = 72;

        private const int MAX_RESULTS = 25;

        private const int TOTAL_PADDING_WIDTH =
                    //Start
                    RESULT_ROW_START +
                    RESULT_IMAGE_WIDTH +

                    RESULT_COLUMN_GAP + //Symbol
                    RESULT_COLUMN_GAP + //Offset
                    RESULT_COLUMN_GAP + //RVA

                    //End
                    RESULT_ROW_END +
                    RESULT_ROW_START; //Also functions as end

        private int _defaultWidth;
        private int _defaultHeight;
        private List<GoToResult> _results = new List<GoToResult>();
        private System.Windows.Forms.Timer _deactivateTimer;

        private HBRUSH _hMainBackgroundBrush;
        private HBRUSH _hResultsBackgroundBrush;
        private HBRUSH _hResultsAltBackgroundBrush;
        private HBRUSH _hSelectedResultBrush;
        private HBRUSH _hWhiteBrush;

        private HPEN _hBorderPen;
        private HPEN _hNullPen;

        private HFONT _hFont;
        private HFONT _hFontBold;
        private HFONT _hHeaderFont;
        private int _headerLineHeight;
        private const int _headerColor = 0x8A8A8A;

        private HDC _hMemDC;
        private HBITMAP _hBitmap;

        private int _nameWidth = -1;
        private int _offsetWidth = -1;
        private int _rvaWidth = -1;
        private int _vaWidth = -1;

        private int _selectedItemIndex = -1;

        public int DefaultWidth => _defaultWidth;
        public int DefaultHeight => _defaultHeight;

        internal FileAccessor _fileAccessor;
        private CancellationTokenSource? _lastCTS;

        public GoToDialog()
        {
            InitializeComponent();

            //We set the back color to magenta so that if the background is ever being painted
            //by something in WinForms we're not in control of, it will clearly stand out
            BackColor = Color.Magenta;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            /* For some reason, if we click into the parent form and the goto dialog
             * calls Close() here, this messes up the activation process and our main
             * window ends up not getting focused. As such, we defer decativation via
             * a timer; this seems to work */
            _deactivateTimer = new System.Windows.Forms.Timer();
            _deactivateTimer.Interval = 1;
            _deactivateTimer.Tick += Timer_Tick;

            //Not browsable in the designer
            LostFocus += GoToDialog_Deactivate;

            _defaultWidth = Width;
            _defaultHeight = Height;

#if DEBUG
            var minAllocatedWidth = MIN_NAME_WIDTH + MIN_OFFSET_WIDTH + MIN_RVA_WIDTH + MIN_VA_WIDTH;

            var total = TOTAL_PADDING_WIDTH + minAllocatedWidth;

            Debug.Assert(MIN_RESULTS_WIDTH == total);
#endif
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                textBox1.SelectAll();
            }

            base.OnVisibleChanged(e);
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                GoToDialog_Deactivate(this, EventArgs.Empty);

                return true;
            }
            else if (keyData == Keys.Enter)
            {
                //Take the selected match (if any). Otherwise, do nothing

                if (_selectedItemIndex != -1)
                {
                    var item = _results[_selectedItemIndex];

                    App.RaisePositionChanged(this, item.Offset);

                    //Fix the focus of the current window getting messed up
                    GoToDialog_Deactivate(this, EventArgs.Empty);

                    return true;
                }
            }
            else if (keyData == Keys.Down)
            {
                if (_selectedItemIndex != -1)
                {
                    _selectedItemIndex++;

                    if (_selectedItemIndex >= _results.Count)
                    {
                        _selectedItemIndex = 0;
                    }

                    Invalidate();

                    return true;
                }
            }
            else if (keyData == Keys.Up)
            {
                if (_selectedItemIndex != -1)
                {
                    _selectedItemIndex--;

                    if (_selectedItemIndex < 0)
                    {
                        _selectedItemIndex = _results.Count - 1;
                    }

                    Invalidate();

                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _hMainBackgroundBrush = Gdi32.CreateSolidBrush(new COLORREF(238, 238, 242)); //#EEEEF2
            _hResultsBackgroundBrush = Gdi32.CreateSolidBrush(new COLORREF(246, 246, 246)); //#F6F6F6
            _hResultsAltBackgroundBrush = Gdi32.CreateSolidBrush(new COLORREF(0xECECEC));

            _hSelectedResultBrush = Gdi32.CreateSolidBrush(new COLORREF(201, 222, 245)); //#C9DEF5
            _hWhiteBrush = Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH);

            _hBorderPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(204, 206, 219)); //#CCCEDB
            _hNullPen = (HPEN) (nint) Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.NULL_PEN);

            var hdc = User32.GetDCEx(Handle, default, GET_DCX_FLAGS.DCX_CACHE);

            _hMemDC = Gdi32.CreateCompatibleDC(hdc);

            _hFont = Font.ToHfont();
            _hFontBold = new Font(Font, FontStyle.Bold).ToHfont();

            var headerFont = new Font("Segoe UI", 8.0f);
            _hHeaderFont = headerFont.ToHfont();

            Gdi32.SelectObject(hdc, _hHeaderFont);

            Gdi32.GetTextExtentPoint32W(hdc, "A", 1, out var headerSize);
            _headerLineHeight = headerSize.cy;

            User32.ReleaseDC(Handle, hdc);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_hMemDC != default)
            {
                Gdi32.DeleteDC(_hMemDC);
                _hMemDC = default;
            }

            if (_hBitmap != default)
            {
                Gdi32.DeleteObject(_hBitmap);
                _hBitmap = default;
            }

            DeleteObject(ref _hMainBackgroundBrush);
            DeleteObject(ref _hResultsBackgroundBrush);
            DeleteObject(ref _hResultsAltBackgroundBrush);
            DeleteObject(ref _hSelectedResultBrush);
            //White brush is a stock object and doesn't need deleting

            DeleteObject(ref _hBorderPen);
            DeleteObject(ref _hNullPen);

            DeleteObject(ref _hFont);
            DeleteObject(ref _hFontBold);
            DeleteObject(ref _hHeaderFont);

            base.OnHandleDestroyed(e);
        }

        private void DeleteObject(ref HBRUSH hBrush)
        {
            if (hBrush != default)
            {
                Gdi32.DeleteObject(hBrush);
                hBrush = default;
            }
        }

        private void DeleteObject(ref HPEN hPen)
        {
            if (hPen != default)
            {
                Gdi32.DeleteObject(hPen);
                hPen = default;
            }
        }

        private void DeleteObject(ref HFONT hFont)
        {
            if (hFont != default)
            {
                Gdi32.DeleteObject(hFont);
                hFont = default;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!IsHandleCreated)
                return;

            var hdc = User32.GetDCEx(Handle, default, GET_DCX_FLAGS.DCX_CACHE);

            CreateBitmap(hdc);

            User32.ReleaseDC(Handle, hdc);
        }

        //Note that if you repeatedly add and remove a search, you get flicker from the window resizing.
        //The top rectangle temporarily turns invisible (perhaps because we ignore erase background).
        //There doesn't seem to be anything we can do about that

        protected unsafe override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            /* The normal dialog should be 418x60. When we're showing search results,
             * an additional rectangle is shown below the main dialog like a dropdown list.
             * Since the bottom pixel of the normal dialog no longer needs to show a border,
             * we only need a 418x59 rectangle up the top */

            if (_results.Count > 0)
            {
                var mainHeight = _defaultHeight - 1;

                //From y-0 to y-mainHeight
                var hMainRgn = Gdi32.CreateRectRgn(0, 0, _defaultWidth, mainHeight);

                Debug.Assert(Height > _defaultHeight);

                var resultsHeight = Height - _defaultHeight + 1;

                //From y-mainHeight to y-totalHeight
                var hResultsRgn = Gdi32.CreateRectRgn(0, mainHeight, Width, Height);

                HRGN hRgn = Gdi32.CreateRectRgn(0, 0, 0, 0);
                Gdi32.CombineRgn(hRgn, hMainRgn, hResultsRgn, RGN_COMBINE_MODE.RGN_OR);

                //Ownership of the combined region passes to Windows
                User32.SetWindowRgn(Handle, hRgn, true);

                Gdi32.DeleteObject(hMainRgn);
                Gdi32.DeleteObject(hResultsRgn);
            }
            else
            {
                var hMainRgn = Gdi32.CreateRectRgn(0, 0, Width, _defaultHeight);

                User32.SetWindowRgn(Handle, hMainRgn, true);
            }
        }

        private void CreateBitmap(HDC hdc)
        {
            var newBitmap = Gdi32.CreateCompatibleBitmap(hdc, Width, Height);

            var oldBitmap = Gdi32.SelectObject(_hMemDC, newBitmap);

            if (_hBitmap != default)
            {
                Gdi32.DeleteObject(oldBitmap);
            }

            _hBitmap = newBitmap;

            Gdi32.SetBkMode(_hMemDC, BACKGROUND_MODE.TRANSPARENT);
        }

        protected override unsafe void OnPaint(PaintEventArgs e)
        {
            var hMemDC = _hMemDC;

            Gdi32.SelectObject(hMemDC, _hMainBackgroundBrush);

            HDC hdc = e.Graphics.GetHdc();

            if (_hBitmap == default)
            {
                CreateBitmap(hdc);
            }

            int contentTop = default;

            if (_results.Count == 0)
            {
                var rect = new RECT(ClientRectangle);

                Gdi32.SelectObject(hMemDC, _hBorderPen);

                //Draw a rectangle around the entire window border
                Gdi32.Rectangle(hMemDC, rect.left, rect.top, rect.right, rect.bottom);
            }
            else
            {
                PaintResultBackground(hMemDC);

                var headerTop = _defaultHeight + RESULT_HEADER_TOP;
                var headerBottom = headerTop + _headerLineHeight;

                contentTop = headerBottom + RESULT_HEADER_BOTTOM_GAP;

                //Draw header

                var xPos = RESULT_TEXT_START;
                var normalColor = Gdi32.SetTextColor(hMemDC, new COLORREF(_headerColor));
                Gdi32.SelectObject(hMemDC, _hHeaderFont);

                DrawText(hMemDC, "Name", xPos, headerTop, xPos + _nameWidth, headerBottom, null, 0);
                xPos += _nameWidth + RESULT_COLUMN_GAP;

                DrawText(hMemDC, "Offset", xPos, headerTop, xPos + _offsetWidth, headerBottom, null, 0);
                xPos += _offsetWidth + RESULT_COLUMN_GAP;

                DrawText(hMemDC, "RVA", xPos, headerTop, xPos + _rvaWidth, headerBottom, null, 0);
                xPos += _rvaWidth + RESULT_COLUMN_GAP;

                DrawText(hMemDC, "VA", xPos, headerTop, xPos + _vaWidth, headerBottom, null, 0);

                Gdi32.SetTextColor(hMemDC, normalColor);

                Gdi32.SelectObject(hMemDC, _hFont);
                Gdi32.SelectObject(hMemDC, _hNullPen);

                using var builder = new ValueStringBuilder();

                for (var i = 0; i < _results.Count; i++)
                {
                    Gdi32.SelectObject(hMemDC, i % 2 == 1 ? _hResultsBackgroundBrush : _hResultsAltBackgroundBrush);

                    if (i == _selectedItemIndex)
                        Gdi32.SelectObject(hMemDC, _hSelectedResultBrush);

                    //The border is at -1 so we need to do -2
                    var top = contentTop + (i * (RESULT_ROW_HEIGHT + RESULT_PADDING));
                    var bottom = top + RESULT_ROW_HEIGHT + 1; //I think bottom needs to be 1 after the actual desired end
                    var left = RESULT_ROW_START;
                    var right = Width - 1;

                    var item = _results[i];
                    item.Rect = new RECT(left, top, right, bottom);

                    Gdi32.Rectangle(hMemDC, left, top, right, bottom);

                    //Draw all columns
                    xPos = RESULT_TEXT_START;

                    const DRAW_TEXT_FORMAT flags = DRAW_TEXT_FORMAT.DT_VCENTER | DRAW_TEXT_FORMAT.DT_SINGLELINE;

                    //Symbol
                    if (item.Name.Length != 0)
                    {
                        DrawText(hMemDC, item.Name, xPos, top, xPos + _nameWidth, bottom, item.NameMatches, flags);
                    }
                    else
                    {
                        Debug.Assert(item.NameWide.Length != 0);
                        DrawText(hMemDC, item.NameWide.AsSpan(), xPos, top, xPos + _nameWidth, bottom, item.NameMatches, flags);
                    }

                    xPos += _nameWidth + RESULT_COLUMN_GAP;

                    //Offset
                    builder.AppendHex((uint) item.Offset);
                    DrawText(hMemDC, builder.AsSpan(), xPos, top, xPos + _offsetWidth, bottom, item.OffsetMatches, flags);
                    builder.Clear();
                    xPos += _offsetWidth + RESULT_COLUMN_GAP;

                    //RVA
                    builder.Append(item.Section);
                    builder.Append(':');
                    builder.AppendHex((uint) item.RVA);
                    DrawText(hMemDC, builder.AsSpan(), xPos, top, xPos + _rvaWidth, bottom, item.RVAMatches, flags);
                    builder.Clear();
                    xPos += _rvaWidth + RESULT_COLUMN_GAP;

                    //VA
                    builder.AppendHex(item.VA);
                    DrawText(hMemDC, builder.AsSpan(), xPos, top, xPos + _vaWidth, bottom, item.VAMatches, flags);
                    builder.Clear();
                }
            }

            Gdi32.SelectObject(hMemDC, _hFont);

            //Draw the label above the textbox. We draw this in GDI rather than have a label control to avoid flicker during redraw
            DrawText(hMemDC, "Enter a symbol, offset, RVA or address:", 4, 6, _defaultWidth, _defaultHeight, null, 0);

            //Draw the rectangle around the textbox
            var textBox = textBox1.Bounds;
            textBox.Inflate(3, 3);

            Gdi32.SelectObject(hMemDC, _hWhiteBrush);
            Gdi32.SelectObject(hMemDC, _hBorderPen);

            Gdi32.Rectangle(hMemDC, textBox.Left, textBox.Top, textBox.Right, textBox.Bottom);

            using var g = Graphics.FromHdc(hMemDC);

            //Draw the image to the left of the textbox. We draw this in GDI rather than have a PictureBox control to avoid flicker during redraw

            g.DrawImage(Resource.magnifier, new Point(7, 31));

            if (_results.Count > 0)
            {
                //Draw images now that the HDC has been released

                for (var i = 0; i < _results.Count; i++)
                {
                    var item = _results[i];

                    var top = contentTop + (i * (RESULT_ROW_HEIGHT + RESULT_PADDING));

                    var bitmap = item.Image;

                    if (bitmap == null)
                        continue;

                    g.DrawImage(bitmap, new Point(RESULT_ROW_START + 11, top + 4));
                }
            }

            Gdi32.BitBlt(hdc, 0, 0, Width, Height, hMemDC, 0, 0, ROP_CODE.SRCCOPY);

            e.Graphics.ReleaseHdc(hdc);
        }

        private unsafe void PaintResultBackground(HDC hMemDC)
        {
            //Draw the background of just the top section
            var rect = new RECT(1, 1, _defaultWidth, _defaultHeight); //The right/bottom are +1 from where you want to go

            Gdi32.SelectObject(hMemDC, _hNullPen);
            Gdi32.Rectangle(hMemDC, rect.left, rect.top, rect.right, rect.bottom);

            //Draw the background of the results section

            Gdi32.SelectObject(hMemDC, _hResultsBackgroundBrush);

            Gdi32.Rectangle(hMemDC, 1, _defaultHeight - 1, _defaultWidth, _defaultHeight + 1);
            Gdi32.Rectangle(hMemDC, 1, _defaultHeight, Width, Height);

            //Draw borders
            Gdi32.SelectObject(hMemDC, _hBorderPen);

            Gdi32.MoveToEx(hMemDC, 0, 0);
            Gdi32.LineTo(hMemDC, _defaultWidth - 1, 0); //Main top
            Gdi32.LineTo(hMemDC, _defaultWidth - 1, _defaultHeight - 1); //Main right
            Gdi32.LineTo(hMemDC, Width - 1, _defaultHeight - 1); //Results top
            Gdi32.LineTo(hMemDC, Width - 1, Height - 1); //Results right
            Gdi32.LineTo(hMemDC, 0, Height - 1); //Results bottom
            Gdi32.LineTo(hMemDC, 0, 0);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_results.Count == 0)
                return;

            var index = HitTestResult(e);

            if (index != -1)
            {
                if (_selectedItemIndex == index)
                    return; //No change

                _selectedItemIndex = index;
                Invalidate();
                return;
            }

            if (_selectedItemIndex != -1)
            {
                _selectedItemIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button != MouseButtons.Left)
                return;

            var index = HitTestResult(e);

            if (index == -1)
                return;

            var item = _results[index];

            App.RaisePositionChanged(this, item.Offset);

            //Fix the focus of the current window getting messed up
            GoToDialog_Deactivate(this, EventArgs.Empty);
        }

        private int HitTestResult(MouseEventArgs e)
        {
            //Hit test to see whether we're over any items

            for (var i = 0; i < _results.Count; i++)
            {
                var item = _results[i];

                if (item.Rect.bottom == 0)
                    continue; //Hasn't been painted yet

                if (item.Rect.Contains(e.X, e.Y))
                    return i;
            }

            return -1;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (_selectedItemIndex != -1)
            {
                _selectedItemIndex = -1;
                Invalidate();
            }
        }

        delegate void DrawTextDelegate(HDC hdc, IntPtr ptr, int read, int toRead, ref RECT r, DRAW_TEXT_FORMAT flags, bool useRanges);

        private unsafe void DrawText(
            HDC hdc,
            FixedUtf8String str,
            int left,
            int top,
            int right,
            int bottom,
            List<GoToResult.Range>? matches,
            DRAW_TEXT_FORMAT flags)
        {
            DrawText(
                hdc,
                (IntPtr) str.Value,
                str.Length,
                left,
                top,
                right,
                bottom,
                matches,
                flags,
                static (HDC hdc, IntPtr ptr, int read, int toRead, ref RECT r, DRAW_TEXT_FORMAT flags, bool useRanges) =>
                {
                    var str = (PCSTR) ((byte*) ptr + read);

                    User32.DrawTextA(hdc, str, toRead, ref r, flags);

                    if (useRanges)
                    {
                        SIZE size;
                        Gdi32.GetTextExtentPoint32A(hdc, str, toRead, &size);

                        r.left += size.cx;
                    }
                }
            );
        }

        private unsafe void DrawText(
            HDC hdc,
            ReadOnlySpan<char> str,
            int left,
            int top,
            int right,
            int bottom,
            List<GoToResult.Range>? matches,
            DRAW_TEXT_FORMAT flags)
        {
            fixed (char* c = str)
            {
                DrawText(
                    hdc,
                    (IntPtr) c,
                    str.Length,
                    left,
                    top,
                    right,
                    bottom,
                    matches,
                    flags,
                    static (HDC hdc, IntPtr ptr, int read, int toRead, ref RECT r, DRAW_TEXT_FORMAT flags, bool useRanges) =>
                    {
                        var str = (char*) ptr + read;

                        User32.DrawTextW(hdc, str, toRead, ref r, flags);

                        if (useRanges)
                        {
                            SIZE size;
                            Gdi32.GetTextExtentPoint32W(hdc, str, toRead, &size);

                            r.left += size.cx;
                        }
                    }
                );
            }
        }

        private unsafe void DrawText(
            HDC hdc,
            IntPtr str,
            int strLen,
            int left,
            int top,
            int right,
            int bottom,
            List<GoToResult.Range>? matches,
            DRAW_TEXT_FORMAT flags,
            DrawTextDelegate drawAction)
        {
            var r = new RECT(left, top, right, bottom);

            if (matches != null)
            {
                var read = 0;

                for (var i = 0; i < matches.Count; i++)
                {
                    var nextRange = matches[i];

                    if (read < nextRange.Start)
                    {
                        var toRead = nextRange.Start - read;

                        drawAction(hdc, str, read, toRead, ref r, flags, true);
                        read += toRead;
                    }

                    var oldFont = Gdi32.SelectObject(hdc, _hFontBold);

                    drawAction(hdc, str, read, nextRange.Length, ref r, flags, true);

                    read += nextRange.Length;

                    Gdi32.SelectObject(hdc, oldFont);
                }

                if (read < strLen)
                {
                    var toRead = strLen - read;

                    drawAction(hdc, str, read, toRead, ref r, flags, true);
                }
            }
            else
            {
                drawAction(hdc, str, 0, strLen, ref r, flags, false);
            }
        }

        private void GoToDialog_Deactivate(object? sender, EventArgs e)
        {
            _deactivateTimer.Start();
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            switch ((WM) m.Msg)
            {
                case WM.WM_ERASEBKGND:
                    m.Result = (IntPtr) 1;
                    return;
            }

            base.WndProc(ref m);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _deactivateTimer.Stop();

            Visible = false;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (_lastCTS != null)
            {
                _lastCTS.Cancel();
                _lastCTS = null;
            }

            if (textBox1.Text.Length == 0)
            {
                if (_results.Count > 0)
                {
                    _results.Clear();

                    SuspendLayout();

                    Width = _defaultWidth;
                    Height = _defaultHeight;

                    ResumeLayout();
                }
            }
            else
            {
                //Kick off a search on a background thread

                var text = textBox1.Text;

                _lastCTS = new CancellationTokenSource();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    SearchThreadProc(text);
                });
            }
        }
        private unsafe void SearchThreadProc(string str)
        {
            var localCTS = _lastCTS;

            if (localCTS == null || localCTS.IsCancellationRequested)
                return;

            //Can't use ref struct in callback
            var results = new List<GoToResult>();
            if (str.StartsWith("0x"))
            {
                //It's definitely hex. Can't be a symbol

                TryProcessHex(results, str.AsSpan(2));
            }
            else
            {
                var ptr = Marshal.StringToHGlobalAnsi(str);

                TryProcessHex(results, str);
                try
                {
                    fixed (char* c = str)
                    {
                        var utf8Str = new FixedUtf8String((byte*) ptr, str.Length);
                        var utf16Str = new FixedUtf16String(c, str.Length);

                        _fileAccessor.EnumerateEntitiesMatchingName(utf8Str, utf16Str, (entity, index, length) =>
                        {
                            ref var sectionAccessor = ref _fileAccessor.SectionAccessors[entity.SectionAccessorIndex];

                            var result = new GoToResult
                            {
                                Name = entity.Name,
                                NameWide = entity.NameWide,
                                Section = sectionAccessor.Name,
                                NameMatches = new List<GoToResult.Range>
                                {
                                    new GoToResult.Range { Start = index, Length = length }
                                }
                            };

                            result.ComputeScore(str.Length);

                            //If we're a loaded image, that means target address is an RVA and we need to convert it to a physical offset
                            if (_fileAccessor.IsLoaded)
                                throw new NotImplementedException();

                            result.Offset = entity.TargetAddress;

                            if (!_fileAccessor.TryGetVirtualAddress(sectionAccessor, entity.TargetAddress, out var rva))
                                rva = entity.TargetAddress;

                            result.Offset = entity.TargetAddress;
                            result.RVA = rva;

                            if (_fileAccessor.ImageBase != 0)
                                result.VA = (ulong) (_fileAccessor.ImageBase + rva);

                            switch (entity.ViewByte->Kind)
                            {
                                case ViewByteKind.Code:
                                    result.Image = Resource.MethodPublic_16_16;
                                    break;

                                case ViewByteKind.Data:
                                    switch (entity.ViewByte->DataKind)
                                    {
                                        case ViewByteDataKind.Struct:
                                            result.Image = Resource.StructurePublic_16_16;
                                            break;

                                        case ViewByteDataKind.Unknown:
                                            break;

                                        case ViewByteDataKind.String:
                                            result.Image = Resource.edit_style;
                                            break;

                                        case ViewByteDataKind.Integer:
                                        case ViewByteDataKind.Decimal:
                                            result.Image = Resource.FieldPublic_16_16;
                                            break;

                                        default:
                                            throw new NotImplementedException();
                                    }

                                    break;

                                default:
                                    throw new NotImplementedException();
                            }

                            results.Add(result);

                            return results.Count < MAX_RESULTS;
                        });
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            if (results.Count > 0)
            {
                results.Sort((a, b) => -a.Score.CompareTo(b.Score));

                BeginInvoke((Action) (() =>
                {
                    ApplyResults(results);
                }));
            }
            else
            {
                BeginInvoke(() =>
                {
                    _results.Clear();

                    SuspendLayout();

                    Width = _defaultWidth;
                    Height = _defaultHeight;

                    Invalidate();

                    ResumeLayout();
                });
            }
        }


        private unsafe void TryProcessHex(List<GoToResult> results, ReadOnlySpan<char> str)
        {
            if (!ulong.TryParse(str, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hexValue))
                return;

            var fileAccessor = App.FileAccessor;

            Debug.Assert(fileAccessor.ImageBase != 0);

            //If the number is greater than the base address, it's definitely a VA
            if (hexValue > (ulong) fileAccessor.ImageBase)
            {
                var rva = (int) (hexValue - (ulong) fileAccessor.ImageBase);
            //Could either be a physical offset, or an RVA. If the image is virtual, offsets _are_ RVAs
            //If we can find an exact match for the specified value, we should immediately return that

            if (hexValue <= uint.MaxValue)
            {
                //In PE Files, all physical offsets must be a uint32. PDB files recently introduced support for generating
                //files >4gb. I don't know what other crazy changes would be needed to support those, so for now we only support
                //32-bit sized lookups

                var offsetOrRVA = (int) hexValue;

                if (fileAccessor.TryGetViewByte(offsetOrRVA, out var pViewByte, out var offsetSectionAccessorIndex))
                {
                    //It's definitely an offset

                    var offsetEntity = fileAccessor.GetEntity(offsetOrRVA);
                    ref var offsetSectionAccessor = ref _fileAccessor.SectionAccessors[offsetEntity.SectionAccessorIndex];
                    if (fileAccessor.TryGetTargetAddress(offsetOrRVA, out var targetAddress, out var rvaSectionIndex))
                    {
                        //It could either be an offset or an RVA

                        //TryGetViewByte returns a section accessor index, while TryGetTargetAddress returns an IMAGE_SECTION_HEADER index
                        if (offsetOrRVA == targetAddress && offsetSectionAccessorIndex - 1 == rvaSectionIndex)
                        {
                            //Regardless of whether we interpret this value as an offset or an RVA, it points to the same location

                            AddHexResult(offsetEntity, offsetOrRVA, offsetOrRVA, offsetSectionAccessor.Name, results);
                        }
                        else
                        {
                            //We get different locations depending on whether we treat this value as an offset or an RVA
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        //The value doesn't make sense when interpreted as an RVA, but it is a valid offset

                        AddHexResult(offsetEntity, offsetOrRVA, offsetOrRVA, offsetSectionAccessor.Name, results);
                    }
                }
                else
                {
                    //The value doesn't make sense when interpreted as an offset, try and interpret as an RVA
                    
                    if (fileAccessor.TryGetTargetAddress(offsetOrRVA, out var targetAddress, out var rvaSectionIndex))
                    {
                        //The value makes sense as an RVA
                        throw new NotImplementedException();
                    }
                }

                if (results.Count >= MAX_RESULTS)
                    return;

                //Try and find "fuzzy matches", wherein the value we're looking for exists at any location 
                var sectionAccessors = fileAccessor.SectionAccessors;

                //Compute the nibble count and mask, which will be common to all searches

                var maskNibbles = 0;

                var tmp = offsetOrRVA;

                do
                {
                    maskNibbles++;
                    tmp >>= 4;
                } while (tmp != 0) ;

                /* Our goal is to create a "sliding window" for inspecting sequences of nibbles within a number.
                 * e.g. suppose we're looking for 0x1008 and are currently inspecting the number 0x23100868. We
                 * start by counting the number of nibbles in the number (4). We then create a bitmask that will
                 * match ths number of characters: 1 << (4 * 4) == 0x10000. Subtracting 1 from this gives us 0xFFFF,
                 * which will then mask out anything above the bottom 4 nibbles we're inspecting.
                 * 
                 * When the inspection loop then runs, we shift all the numbers in from the right to the left. So
                 * the sequence will be 2, 23, 231, 2310 and then when we get to 23100 the mask clears the "2" and
                 * leaves us with 3100, thereby allowing us to continue sliding numbers across until we potentially
                 * see the number 0x1008 */

                var mask = (1u << (maskNibbles * 4)) - 1;

                for (var i = 0; i < sectionAccessors.Length; i++)
                {
                    ref var sectionAccessor = ref sectionAccessors[i];

                    foreach (var match in EnumerateAddressesWithPattern((uint) sectionAccessor.StartAddress, (uint) sectionAccessor.EndAddress, maskNibbles, mask, (uint) offsetOrRVA))
                    {
                        if (match == offsetOrRVA)
                            continue;

                        var entity = fileAccessor.GetEntity(match);

                        if (!fileAccessor.TryGetVirtualAddress(sectionAccessor, match, out var rva))
                            rva = match;

                        AddHexResult(entity, match, rva, sectionAccessor.Name, results);

                        if (results.Count >= MAX_RESULTS)
                            return;
                    }
        private unsafe void AddHexResult(ViewEntity entity, int offset, int rva, string sectionName, List<GoToResult> results)
        {
            FixedUtf8String name;
            FixedUtf16String nameWide;

            ViewEntity headEntity = entity;

            if (entity.ViewByte->Kind == ViewByteKind.Body)
            {
                //Rewind to find the head. Way out for split bytes!

                var pViewByte = entity.ViewByte;

                while (true)
                {
                    if (pViewByte->Kind == ViewByteKind.Body)
                    {
                        if (pViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                            throw new NotImplementedException();

                        pViewByte--;
                    }
                    else
                        break;
                }

                var headOffset = (int) (entity.ViewByte - pViewByte);
                headEntity = _fileAccessor.GetEntity(offset - headOffset);

                Debug.Assert(headEntity.ViewByte->Kind != ViewByteKind.Body);
            }

            if (headEntity.ViewByte->Kind == ViewByteKind.Code && (headEntity.Name == (FixedUtf8String) default && !headEntity.ViewByte->IsFunction))
            {
            }
            else
            {
                name = headEntity.Name;
                nameWide = headEntity.NameWide;
            }

            Debug.Assert(_fileAccessor.ImageBase != 0);

            results.Add(new GoToResult
            {
                Name = name,
                NameWide = nameWide,
                Offset = offset,
                RVA = rva,
                Section = sectionName,
                VA = (ulong) (_fileAccessor.ImageBase + rva)
            });
        }

        private IEnumerable<int> EnumerateAddressesWithPattern(
            uint startAddress,
            uint endAddress,
            int maskNibbles,
            uint mask,
            uint pattern)
        {
            const int maxNibbles32 = 8;

            for (var i = startAddress; i < endAddress; i++)
            {
                uint window = 0;

                for (int j = maxNibbles32 - 1; j >= 0; j--)
                {
                    // Extract nibble from value (MSB → LSB)
                    uint nibble = (i >> (j * 4)) & 0xF;

                    // Slide window
                    window = ((window << 4) | nibble) & mask;

                    // Compare
                    if (window == pattern)
                        yield return (int) i;
                }
            }
        }

        private unsafe void ApplyResults(List<GoToResult> results)
        {
            //Compute the expected minimum width

            var nameWidth = MIN_NAME_WIDTH;
            var offsetWidth = MIN_OFFSET_WIDTH;
            var rvaWidth = MIN_RVA_WIDTH;
            var vaWidth = MIN_VA_WIDTH;

            //Figure out the length of each column

            //Provides a mechanism for building strings without allocating
            using var builder = new ValueStringBuilder();

            var hdc = User32.GetDCEx(Handle, default, GET_DCX_FLAGS.DCX_CACHE);
            Gdi32.SelectObject(hdc, _hFont);

            foreach (var item in results)
            {
                if (item.Name.Length != 0)
                {
                    nameWidth = Math.Max(nameWidth, MeasureString(hdc, item.Name, item.NameMatches));
                }
                else
                {
                    Debug.Assert(item.NameWide.Length != 0);
                    nameWidth = Math.Max(nameWidth, MeasureString(hdc, item.NameWide.AsSpan(), item.NameMatches));
                }

                //Offset
                builder.AppendHex((uint) item.Offset);
                offsetWidth = Math.Max(offsetWidth, MeasureString(hdc, builder.AsSpan(), item.OffsetMatches));
                builder.Clear();

                //RVA
                builder.Append(item.Section);
                builder.Append(':');
                builder.AppendHex((uint) item.RVA);
                rvaWidth = Math.Max(rvaWidth, MeasureString(hdc, builder.AsSpan(), item.RVAMatches));
                builder.Clear();

                //VA
                builder.AppendHex((ulong) item.VA);
                vaWidth = Math.Max(vaWidth, MeasureString(hdc, builder.AsSpan(), item.VAMatches));
                builder.Clear();
            }

            _results = results;

            _nameWidth = nameWidth;
            _offsetWidth = offsetWidth;
            _rvaWidth = rvaWidth;
            _vaWidth = vaWidth;

            _selectedItemIndex = 0;

            SuspendLayout();

            var requiredHeight =
                //Header
                _defaultHeight + RESULT_HEADER_TOP + _headerLineHeight + RESULT_HEADER_BOTTOM_GAP +
                ((RESULT_ROW_HEIGHT + RESULT_PADDING) * _results.Count) + BORDER_WIDTH;

            Height = requiredHeight;

            var newWidth = TOTAL_PADDING_WIDTH + _nameWidth + _offsetWidth + _rvaWidth + _vaWidth;

            Width = newWidth;

            Invalidate();

            ResumeLayout();

            User32.ReleaseDC(Handle, hdc);
        }

        private unsafe int MeasureString(HDC hdc, ReadOnlySpan<char> str, List<GoToResult.Range>? ranges)
        {
            fixed (char* c = str)
            {
                return MeasureString(hdc, (IntPtr) c, str.Length, ranges, static (hdc, p, read, toRead) =>
                {
                    SIZE size;
                    Gdi32.GetTextExtentPoint32W(hdc, (char*) p + read, toRead, &size);
                    return size.cx;
                });
            }
        }

        private unsafe int MeasureString(HDC hdc, FixedUtf8String str, List<GoToResult.Range>? ranges)
        {
            return MeasureString(hdc, (IntPtr) str.Value, str.Length, ranges, static (hdc, p, read, toRead) =>
            {
                SIZE size;
                Gdi32.GetTextExtentPoint32A(hdc, (PCSTR) ((byte*) p + read), toRead, &size);
                return size.cx;
            });
        }

        private unsafe int MeasureString(
            HDC hdc,
            IntPtr pStr,
            int strLen,
            List<GoToResult.Range>? ranges,
            Func<HDC, IntPtr, int, int, int> measureAction)
        {
            if (ranges != null)
            {
                var read = 0;
                var totalWidth = 0;

                for (var i = 0; i < ranges.Count; i++)
                {
                    var nextRange = ranges[i];

                    if (read < nextRange.Start)
                    {
                        var toRead = nextRange.Start - read;
                        totalWidth += measureAction(hdc, pStr, read, toRead);
                        read += toRead;
                    }

                    var oldFont = Gdi32.SelectObject(hdc, _hFontBold);

                    totalWidth += measureAction(hdc, pStr, read, nextRange.Length);
                    read += nextRange.Length;

                    Gdi32.SelectObject(hdc, oldFont);
                }

                if (read < strLen)
                {
                    totalWidth += measureAction(hdc, pStr, read, strLen - read);
                }

                return totalWidth;
            }
            else
            {
                return measureAction(hdc, pStr, 0, strLen);
            }
        }
    }
}
