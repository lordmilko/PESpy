using System;
using System.Diagnostics;
using PInvoke;

namespace PESpy
{
    public class Win32Graphics : IGraphics
    {
        private HWND _hWnd;

        private HDC _hMemDC;
        private HFONT _hFont;
        private HBITMAP _hBitmap;

        private HBRUSH _hWhiteBrush; //Stock object, doesn't need disposin

        public int LineHeight { get; private set; }

        public HDC MemDC => _hMemDC;

        internal Win32Graphics(HWND hWnd)
        {
            _hWnd = hWnd;
        }

        //Called by WmCreate
        public unsafe void Initialize()
        {
            Debug.Assert(_hWnd != default);
            var hdc = User32.GetDCEx(_hWnd, default, GET_DCX_FLAGS.DCX_CACHE);

            var lf = new LOGFONTW
            {
                lfHeight = -(13 * 96 / 72),
            };

            "Consolas\0".AsSpan().CopyTo(new Span<char>(&lf.lfFaceName, 32));

            var hFont = Gdi32.CreateFontIndirectW(lf);

            int lineHeight;
            Gdi32.SelectObject(hdc, hFont);
            Gdi32.GdiGetCharDimensions(hdc, default, &lineHeight);

            _hFont = hFont;
            LineHeight = lineHeight;
            _hWhiteBrush = Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.WHITE_BRUSH);

            _hMemDC = Gdi32.CreateCompatibleDC(hdc);

            User32.ReleaseDC(_hWnd, hdc);
        }

        public void CreateMemoryBitmap(int width, int height)
        {
            if (_hBitmap != default)
                Gdi32.DeleteObject(_hBitmap);

            var hdc = User32.GetDCEx(_hWnd, default, GET_DCX_FLAGS.DCX_CACHE);

            //Create an off screen bitmap large enough to display a whole number of lines.
            //Thus, even if the screen is only capable of showing a fractional number, we won't
            //have to regenerate the partially visible lines, we can just blit them into view

            _hBitmap = Gdi32.CreateCompatibleBitmap(hdc, width, height);
            Gdi32.SelectObject(_hMemDC, _hFont);
            Gdi32.SelectObject(_hMemDC, _hBitmap);

            User32.ReleaseDC(_hWnd, hdc);
        }

        public void FillBackground(in RECT rect) =>
            User32.FillRect(_hMemDC, rect, _hWhiteBrush);

        public unsafe void DrawText(ReadOnlySpan<char> str, int left, int top, ref int right, int fontHeight)
        {
            var hMemDC = MemDC;

            fixed (char* c = str)
            {
                SIZE size;
                Gdi32.GetTextExtentPoint32W(hMemDC, (PCWSTR) c, str.Length, &size);

                right += size.Width;
                var rect = new RECT(left, top, right, top + fontHeight);

                User32.DrawTextW(hMemDC, c, str.Length, &rect, default);
            }
        }

        public void ScrollMemDC(int destX, int destY, int width, int height, int sourceX, int sourceY) =>
            Gdi32.BitBlt(MemDC, destX, destY, width, height, MemDC, sourceX, sourceY, ROP_CODE.SRCCOPY);

        public unsafe int GetVerticalScrollThumb()
        {
            var lpsi = new SCROLLINFO
            {
                cbSize = sizeof(SCROLLINFO),
                fMask = SCROLLINFO_MASK.SIF_TRACKPOS
            };

            User32.GetScrollInfo(_hWnd, SCROLLBAR_CONSTANTS.SB_VERT, ref lpsi);

            //When we scroll normally, the track position is set to the top-most visible line
            //in the view. When we're tragging the thumb, I imagine that Windows will automatically
            //assign positions to it between min and max
            var pos = lpsi.nTrackPos;

            return pos;
        }

        public void SetScrollInfo(SCROLLBAR_CONSTANTS scrollBar, in SCROLLINFO scrollInfo) =>
            User32.SetScrollInfo(_hWnd, scrollBar, scrollInfo, false);

        public void CommitMemDC(int destX, int destY, int width, int height, int sourceX, int sourceY, HDC destinationDC = default)
        {
            if (destinationDC == default)
            {
                var hdc = User32.GetDCEx(_hWnd, default, GET_DCX_FLAGS.DCX_CACHE);
                Gdi32.BitBlt(hdc, destX, destY, width, height, MemDC, sourceX, sourceY, ROP_CODE.SRCCOPY);
                User32.ReleaseDC(_hWnd, hdc);
            }
            else
            {
                Gdi32.BitBlt(destinationDC, destX, destY, width, height, MemDC, sourceX, sourceY, ROP_CODE.SRCCOPY);
            }
        }

        public void Dispose()
        {
            if (_hMemDC != default)
            {
                Gdi32.DeleteDC(_hMemDC);
                _hMemDC = default;
            }

            //Delete the DC before the bitmap, as the DC may be locking the bitmap
            if (_hBitmap != default)
            {
                Gdi32.DeleteObject(_hBitmap);
                _hBitmap = default;
            }

            if (_hFont != default)
            {
                Gdi32.DeleteObject(_hFont);
                _hFont = default;
            }
        }
    }
}
