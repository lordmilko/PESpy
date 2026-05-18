using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;
using PInvoke;
using ReView;

namespace PESpy.ViewMap
{
    /// <summary>
    /// Provides facilities for painting a layer of a <see cref="ViewMap"/>.
    /// </summary>
    internal struct ViewMapPainter
    {
        private HWND _hWnd;
        internal HDC _hMemDC;
        private HBITMAP _hBitmap;
        private ViewMapPanel _viewMap; //Provides access to the various GDI handles we need in order to paint
        private RECT _clientRect;
        private int _yOffset;

        public bool IsValid;

        public bool IsEmpty => _hBitmap == default;
        public int Width => _clientRect.Width;

        //The DC that is passed in is only used to create a compatible DC; it will be released once the ViewMap has been initialized
        public ViewMapPainter(HWND hWnd, HDC hdc, ViewMapPanel viewMap, int yOffset)
        {
            _hWnd = hWnd;
            _hMemDC = Gdi32.CreateCompatibleDC(hdc);
            _viewMap = viewMap;
            _yOffset = yOffset;

            ConfigureMemDC();

            _clientRect = default;
            _hBitmap = default;
            IsValid = false;
        }

        //Indicates that we need to overwrite the data contained within this painter on next render
        public void Invalidate()
        {
            IsValid = false;
        }

        internal void ConfigureMemDC()
        {
            Gdi32.SelectObject(_hMemDC, _viewMap._hCodeFont);
            Gdi32.SetBkMode(_hMemDC, BACKGROUND_MODE.TRANSPARENT);
            Gdi32.SetTextColor(_hMemDC, new COLORREF(255, 255, 255));
        }

        #region Base

        /// <summary>
        /// Renders the default layout of the <see cref="ViewMap"/>, including the background,
        /// default section colors, and legend. Higher level painters may choose to paint over
        /// the base layer by applying highlights, or painting an arrow to mark our current location
        /// </summary>
        public unsafe void RenderBaseLayer(FileAccessor fileAccessor)
        {
            //We should've retrieved a new bitmap, and we shouldn't be being asked to render if we're already valid
            Debug.Assert(_hBitmap != default);
            Debug.Assert(!IsValid);

            User32.FillRect(_hMemDC, _clientRect, UIElement.DefaultBackgroundBrush);

            var visualSections = _viewMap._visualSections;
            var pixels = _viewMap._pixels;
            var sectionAccessors = fileAccessor.SectionAccessors;

            Debug.Assert(pixels != null);

            //If we don't have any visual sections, that means we had more sections than will fit on the screen, which is an issue
            if (visualSections == null)
                RenderBaseFlat(fileAccessor, pixels);
            else
                RenderBaseSections(fileAccessor, sectionAccessors, visualSections, pixels);

            //Below the main area, draw a legend
            DrawLegend();

            IsValid = true;
        }

        private void RenderBaseFlat(FileAccessor fileAccessor, ViewMapPixel[] pixels)
        {
            DrawPixels(fileAccessor, _hMemDC, xPos: 0, startIndex: 0, endIndex: pixels.Length, pixels, isHighlighted: false);
        }

        private void RenderBaseSections(
            FileAccessor fileAccessor,
            SectionAccessor[] sectionAccessors,
            VisualSection[] visualSections,
            ViewMapPixel[] pixels)
        {
            for (var i = 0; i < visualSections!.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                if (sectionAccessor.IsEmpty)
                    continue;

                ref var visualSection = ref visualSections[i];

                if (visualSection.Width == 0)
                    continue;

                var start = visualSection.PhysicalStartPixel;

                DrawSection(start, false, sectionAccessor, visualSection, fileAccessor, pixels);
            }
        }

        private void DrawLegend()
        {
            var rectTop = ViewMapPanel.COLORLINE_HEIGHT + 3 + _yOffset;
            var rectLeft = 3;
            var rectSideLength = 16;

            for (var i = 0; i < 5; i++)
            {
                string text;

                HPEN hPen;
                HBRUSH hBrush;

                switch (i)
                {
                    case 0:
                        text = "Code";
                        hPen = _viewMap._codePen;
                        hBrush = _viewMap._codeBrush;
                        break;

                    case 1:
                        text = "Data";
                        hPen = _viewMap._dataPen;
                        hBrush = _viewMap._dataBrush;
                        break;

                    case 2:
                        text = "Padding";
                        hPen = _viewMap._paddingPen;
                        hBrush = _viewMap._paddingBrush;
                        break;

                    case 3:
                        text = "External";
                        hPen = _viewMap._externalPen;
                        hBrush = _viewMap._externalBrush;
                        break;

                    case 4:
                        text = "Unknown";
                        hPen = _viewMap._unknownPen;
                        hBrush = _viewMap._unknownBrush;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                var hMemDC = _hMemDC;

                Gdi32.SelectObject(hMemDC, hPen);
                Gdi32.SelectObject(hMemDC, hBrush);

                Gdi32.Rectangle(hMemDC, rectLeft, rectTop, rectLeft + rectSideLength, rectTop + rectSideLength);

                var textStart = rectLeft + rectSideLength + 7;

                Gdi32.SelectObject(hMemDC, _viewMap._hLegendFont);
                Gdi32.SetTextColor(hMemDC, new COLORREF(0, 0, 0));
                var midOffset = (rectSideLength - _viewMap._legendFontHeight) / 2;
                var right = textStart; //Let DrawText just add right onto left
                DrawText(hMemDC, text, textStart, rectTop + midOffset, ref right, _viewMap._legendFontHeight);

                rectLeft = right + 7;
            }
        }

        private unsafe void DrawText(HDC memDC, string str, int left, int top, ref int right, int fontHeight)
        {
            SIZE size;
            Gdi32.GetTextExtentPoint32W(memDC, str, str.Length, out size);

            right += size.Width;
            var rect = new RECT(left, top, right, top + fontHeight);

            fixed (char* c = str)
            {
                User32.DrawTextW(memDC, c, str.Length, &rect, default);
            }
        }

        private unsafe void DrawSection(
            int start,
            bool isHighlighted,
            in SectionAccessor sectionAccessor,
            in VisualSection visualSection,
            FileAccessor fileAccessor,
            ViewMapPixel[] pixels)
        {
            var xPos = start;

            var hMemDC = _hMemDC;

            var startIndex = visualSection.PhysicalStartPixel;
            var endIndex = startIndex + visualSection.Width;

            DrawPixels(fileAccessor, hMemDC, xPos, startIndex, endIndex, pixels, isHighlighted);

            //Draw the name of the section

            //We need right to be xPos + the actual width, else the edge of it might get clipped
            var minRight = xPos;
            DrawText(hMemDC, sectionAccessor.Name, left: visualSection.PhysicalStartPixel, top: _yOffset, ref minRight, _viewMap._codeFontHeight);
        }

        private unsafe void DrawPixels(
            FileAccessor fileAccessor,
            HDC hMemDC,
            int xPos,
            int startIndex,
            int endIndex,
            ViewMapPixel[] pixels,
            bool isHighlighted)
        {
            //For each pixel, draw the type of data it contains
            for (var j = startIndex; j < endIndex; j++)
            {
                var pixel = pixels[j];

                var pViewByte = pixel.pStartViewByte;

                var pen = GetUsagePen(pViewByte, isHighlighted, fileAccessor, pixel.StartAddress);

                Gdi32.SelectObject(hMemDC, pen);

                Gdi32.MoveToEx(hMemDC, xPos, 1 + _yOffset);
                Gdi32.LineTo(hMemDC, xPos, ViewMapPanel.COLORLINE_HEIGHT + _yOffset - 1);

                xPos++;
            }
        }

        #endregion
        #region Highlight

        public void DrawHighlights(FileAccessor fileAccessor)
        {
            Debug.Assert(_hBitmap != default);
            Debug.Assert(_viewMap._highlightedSection != -1);
            Debug.Assert(!IsValid);

            var pixels = _viewMap._pixels;
            Debug.Assert(pixels != null);

            if (_viewMap._visualSections == null)
            {
                //Highlight everything

                DrawPixels(fileAccessor, _hMemDC, xPos: 0, startIndex: 0, endIndex: pixels.Length, pixels, isHighlighted: true);

                DrawHighlightOutline(_hMemDC, 0, pixels.Length, ViewMapPanel.COLORLINE_HEIGHT);
            }
            else
            {
                ref var sectionAccessor = ref fileAccessor.SectionAccessors[_viewMap._highlightedSection];
                ref var visualSection = ref _viewMap!._visualSections![_viewMap._highlightedSection];

                var start = visualSection.PhysicalStartPixel;

                DrawSection(start, isHighlighted: true, sectionAccessor, visualSection, fileAccessor, pixels);

                DrawHighlightOutline(_hMemDC, visualSection.PhysicalStartPixel, visualSection.Width, ViewMapPanel.COLORLINE_HEIGHT);
            }

            IsValid = true;
        }

        private void DrawHighlightOutline(HDC hdc, int start, int width, int height)
        {
            Gdi32.SelectObject(hdc, _viewMap._outlineSectionHighlightPen);
            Gdi32.SelectObject(hdc, (HBRUSH) (IntPtr) Gdi32.GetStockObject(GET_STOCK_OBJECT_FLAGS.HOLLOW_BRUSH));
            Gdi32.Rectangle(hdc, start, _yOffset, start + width, height + _yOffset);
        }

        #endregion
        #region Arrow

        //When the arrow is dragged, assert we're highlighted; if it's the same section as before, binary search from the previous position in the direction of movement
        //to find the new X coordinate. Otherwise, it's a new section, binary search to find it. If the arrow moved because of a change in the view control, binary search the
        //whole thing to find where the arrow goes
        public unsafe void DrawArrow(int arrowXPos)
        {
            Debug.Assert(arrowXPos != -1);

            //Draw an arrow indicating the current position
            const int numPoints = 7;
            var points = stackalloc POINT[numPoints];
            var arrowLeft = arrowXPos - 1; //Subtract 1 so that the middle points at the actual value
            var arrowTop = 2 + _yOffset;
            points[0] = new POINT { x = arrowLeft, y = arrowTop }; //Top left
            points[1] = new POINT { x = arrowLeft + 2, y = arrowTop }; //Top right
            points[2] = new POINT { x = arrowLeft + 2, y = arrowTop + 4 }; //Bottom right

            points[3] = new POINT { x = arrowLeft + 3, y = arrowTop + 5 }; //Head right
            points[4] = new POINT { x = arrowLeft + 1, y = arrowTop + 7 }; //head tip
            points[5] = new POINT { x = arrowLeft - 1, y = arrowTop + 5 }; //Head left

            points[6] = new POINT { x = arrowLeft, y = arrowTop + 4 }; //Bottom left

            var hMemDC = _hMemDC;

            Gdi32.SelectObject(hMemDC, _viewMap._arrowPen);
            Gdi32.SelectObject(hMemDC, _viewMap._arrowBrush);
            Gdi32.Polygon(hMemDC, points, numPoints);
        }

        #endregion

        private unsafe HPEN GetUsagePen(ViewByte* pViewByte, bool isHighlighted, FileAccessor fileAccessor, long startAddress)
        {
            switch (pViewByte->Kind)
            {
                case ViewByteKind.Code:
                    return isHighlighted ? _viewMap._codeHighlightPen : _viewMap._codePen;

                case ViewByteKind.Data:

                    if (pViewByte->DataKind == ViewByteDataKind.Padding)
                        return isHighlighted ? _viewMap._paddingHighlightPen : _viewMap._paddingPen;
                    else
                    {
                        if (fileAccessor.TryGetStructKind(startAddress, out var kind))
                        {
                            switch (kind)
                            {
                                case ViewKind.ImageThunkData:
                                    return isHighlighted ? _viewMap._externalHighlightPen : _viewMap._externalPen;

                                default:
                                    return isHighlighted ? _viewMap._dataHighlightPen : _viewMap._dataPen;
                            }
                        }
                        else
                            return isHighlighted ? _viewMap._dataHighlightPen : _viewMap._dataPen;
                    }

                case ViewByteKind.Unknown:
                case ViewByteKind.Body: //If we're painting during analysis, something we thought was unknown might have turned into body on this frame
                    //C0C0C0
                    return isHighlighted ? _viewMap._unknownHighlightPen : _viewMap._unknownPen;

                default:
                    throw new NotImplementedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(in ViewMapPainter painter) => CopyTo(painter._hMemDC);

        public void CopyTo(HDC hdc)
        {
            var clientRect = _clientRect;

            //Now blit it!
            Gdi32.BitBlt(
                hdc, //Copy to the screen...
                x: clientRect.X, //Destination X
                y: clientRect.Y, //Destination Y
                clientRect.Width,
                clientRect.Height,
                _hMemDC, //...from memory
                x1: clientRect.X, //Source X
                y1: clientRect.Y, //Source Y
                ROP_CODE.SRCCOPY
            );
        }

        public void Resize(HDC hdc, in RECT clientRect)
        {
            if (_hBitmap != default)
            {
                //The hBitmap needs to be regenerated
                Gdi32.DeleteObject(_hBitmap);
            }

            _clientRect = clientRect;

            var hMemDC = _hMemDC;

            //You have to create a bitmap to be able to use your compatible DC
            _hBitmap = Gdi32.CreateCompatibleBitmap(hdc, clientRect.Width, clientRect.Height);
            Gdi32.SelectObject(hMemDC, _hBitmap);

            ConfigureMemDC();

            IsValid = false;
        }

        //Deletes the DC and bitmap
        public void Dispose()
        {
            if (_hMemDC != default)
            {
                Gdi32.DeleteDC(_hMemDC);
                _hMemDC = default;
            }

            //The bitmap must be disposed _after_ the DC, as the DC is still locking it
            if (_hBitmap != default)
            {
                Gdi32.DeleteObject(_hBitmap);
                _hBitmap = default;
            }
        }
    }
}
