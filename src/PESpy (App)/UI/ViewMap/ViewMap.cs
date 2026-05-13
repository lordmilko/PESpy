using System;
using System.Buffers;
using System.Diagnostics;
using PESpy.View;
using PESpy.View.Builder;
using PInvoke;
using ReView;

namespace PESpy
{
    internal class ViewMap : UIElement
    {
        //Each section should be at least 3 pixels so that I can draw an outline around the section when I hover over it
        private const int MIN_WIDTH = 3;
        internal const int COLORLINE_HEIGHT = 23;

        internal VisualSection[]? _visualSections;
        private int _scale = 1;
        private bool _isMaxScroll;
        private long _arrowAddress;
        private int _arrowXPos;
        private int _arrowSectionIndex;
        internal int _highlightedSection = -1; //Gets the index of the section that is currently highlighted (or -1 if no section is currently highlighted)
        private bool _wasLeftMouseDown;

        //Used to create an extra gap above the control
        private int _yOffset = 0;

        private NativeTooltip _tooltip;
        private int _lastToolTipPos;

        private ViewMapPainter _basePainter; //Stores the unhighlighted content
        private ViewMapPainter _highlightPainter; //Stores the current highlighted area
        private ViewMapPainter _screenPainter; //Stores everything we're working on as we prepare to blit to the screen. This should just be the contents of base and/or highlight painter + the arrow

        private FileOpenedEventKind _lastFileOpenEvent;

        #region Brushes / Pens

        internal HPEN _dataPen;
        internal HPEN _dataHighlightPen;
        internal HBRUSH _dataBrush;

        internal HPEN _codePen;
        internal HPEN _codeHighlightPen;
        internal HBRUSH _codeBrush;

        internal HPEN _externalPen;
        internal HPEN _externalHighlightPen;
        internal HBRUSH _externalBrush;

        internal HPEN _unknownPen;
        internal HPEN _unknownHighlightPen;
        internal HBRUSH _unknownBrush;

        internal HPEN _paddingPen;
        internal HPEN _paddingHighlightPen;
        internal HBRUSH _paddingBrush;

        internal HPEN _arrowPen;
        internal HBRUSH _arrowBrush;

        internal HPEN _outlineSectionHighlightPen;

        #endregion
        #region Fonts

        internal HFONT _hCodeFont;
        internal int _codeFontHeight;

        internal HFONT _hLegendFont;
        internal int _legendFontHeight;

        #endregion

        public ViewMap(out ViewMap field)
        {
            field = this;

            _tooltip = new NativeTooltip
            {
                //Show the tooltip even if PESpy is not the active window; if they hover over the view map,
                //show the tooltip
                ShowAlways = true,
                AutomaticDelay = 0
            };

            App.FileOpened += App_FileOpened;
            App.FileClosed += App_FileClosed;

            App.PositionChanged += App_PositionChanged;
        }

        private unsafe void App_FileOpened(object? sender, FileOpenedEventArgs e)
        {
            if (!Visible)
            {
                _lastFileOpenEvent = e.EventKind;
                return;
            }

            switch (e.EventKind)
            {
                case FileOpenedEventKind.OpenAccessor:
                    ProcessOpenAccessor();
                    break;

                case FileOpenedEventKind.AnalysisComplete:
                    ProcessAnalysisComplete();
                    break;
            }
        }

        private unsafe void ProcessOpenAccessor()
        {
            ComputeRegions();

            BeginInvoke(() =>
            {
                //Invalidate just in case we're already visible?
                ForceInvalidate();

                //Install a timer to periodically refresh the map while we're still in the process of loading
                SetTimer(1, 1);
            });
        }

        private void ProcessAnalysisComplete()
        {
            //Stop the timer, and do one final refresh
            StopTimer();

            ComputeRegions();

            ForceInvalidate();
        }

        protected override void OnVisibleChanged()
        {
            base.OnVisibleChanged();

            if (Visible && _lastFileOpenEvent != 0)
            {
                OnWindowPosChanged();

                switch (_lastFileOpenEvent)
                {
                    case FileOpenedEventKind.OpenAccessor:
                        ProcessOpenAccessor();
                        break;

                    case FileOpenedEventKind.AnalysisComplete:
                        ProcessAnalysisComplete();
                        break;
                }

                _lastFileOpenEvent = default;
            }
        }

        private void ForceInvalidate()
        {
            //Make sure the font is set. I think there's some sort of race here that's causing an issue
            _basePainter.ConfigureMemDC();

            _basePainter.Invalidate();
            _highlightPainter.Invalidate();

            Invalidate();
        }

        private void StopTimer()
        {
            KillTimer(1);
        }

        protected override void OnTimer(nuint id)
        {
            ComputeRegions();
            ForceInvalidate();
        }

        private void App_FileClosed(object? sender, EventArgs e)
        {
            _lastFileOpenEvent = default;

            //Stop the refresh timer if it's active
            StopTimer();
        }

        private void App_PositionChanged(object? sender, int e)
        {
            if (sender == this || _visualSections == null)
                return;

            using var scope = App.AcquireFileAccessor();

            if (scope.FileAccessor == null)
                return;

            if (e != _arrowAddress)
            {
                var sectionAccessors = scope.FileAccessor.SectionAccessors;

                for (var i = _visualSections.Length - 1; i >= 0; i--)
                {
                    ref var sectionAccessor = ref sectionAccessors[i];

                    if (e >= sectionAccessor.StartAddress)
                    {
                        ref var visualSection = ref _visualSections[i];

                        _arrowXPos = visualSection.PhysicalStartPixel + visualSection.GetBestPixel(e, 0, visualSection.Data.Length - 1);
                        break;
                    }
                }

                _arrowAddress = e;

                var hdc = User32.GetDCEx(NativeHandle, default, GET_DCX_FLAGS.DCX_CACHE);

                OnPaint(hdc);

                User32.ReleaseDC(NativeHandle, hdc);
            }
        }

        protected unsafe override void OnHandleCreated()
        {
            var hdc = User32.GetDCEx(NativeHandle, default, GET_DCX_FLAGS.DCX_CACHE);

            #region Brushes / Pens

            var dataColor = new COLORREF(0xB9, 0x7A, 0x57); //Brown
            _dataPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, dataColor);
            _dataHighlightPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(0xCD, 0xA1, 0x89)); //Light Brown
            _dataBrush = Gdi32.CreateSolidBrush(dataColor);

            var codeColor = new COLORREF(0x00, 0xA2, 0xE8); //Blue
            _codePen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, codeColor);
            _codeHighlightPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(0x3C, 0xC4, 0xFF)); //Lighter Blue
            _codeBrush = Gdi32.CreateSolidBrush(codeColor);

            var externalColor = new COLORREF(0xFF, 0xA6, 0xFF); //Pink
            _externalPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, externalColor);
            _externalHighlightPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(0xFF, 0xC0, 0xFF)); //Also pink
            _externalBrush = Gdi32.CreateSolidBrush(externalColor);

            var unknownColor = new COLORREF(0xA3, 0x49, 0xA4);
            _unknownPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, unknownColor);
            _unknownHighlightPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(0xBF, 0x7F, 0xBF));
            _unknownBrush = Gdi32.CreateSolidBrush(unknownColor);

            var paddingColor = new COLORREF(0xC0, 0xC0, 0xC0); //Grey
            _paddingPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, paddingColor);
            _paddingHighlightPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(0xD2, 0xD2, 0xD2)); //Lighter Grey
            _paddingBrush = Gdi32.CreateSolidBrush(paddingColor);

            var arrowColor = new COLORREF(0xFF, 0xFF, 0x7F);
            _arrowPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, arrowColor);
            _arrowBrush = Gdi32.CreateSolidBrush(arrowColor);

            _outlineSectionHighlightPen = Gdi32.CreatePen(PEN_STYLE.PS_SOLID, 1, new COLORREF(0xFF, 0x00, 0x80));

            #endregion
            #region Fonts

            //Code

            var lf = new LOGFONTW
            {
                lfHeight = -10,
            };

            "Consolas\0".AsSpan().CopyTo(new Span<char>(&lf.lfFaceName, 32));

            _hCodeFont = Gdi32.CreateFontIndirectW(lf);

            int lineHeight;
            Gdi32.SelectObject(hdc, _hCodeFont);
            Gdi32.GdiGetCharDimensions(hdc, default, &lineHeight);

            _codeFontHeight = lineHeight;

            //Legend

            lf = new LOGFONTW
            {
                lfHeight = -13,
            };

            "Tahoma\0".AsSpan().CopyTo(new Span<char>(&lf.lfFaceName, 32));

            _hLegendFont = Gdi32.CreateFontIndirectW(lf);

            Gdi32.SelectObject(hdc, _hLegendFont);
            Gdi32.GdiGetCharDimensions(hdc, default, &lineHeight);

            _legendFontHeight = lineHeight;

            #endregion

            //The HDC is just used to create a compatible DC; the painter must not hold on to our HDC, because we're about to release it
            _basePainter = new ViewMapPainter(NativeHandle, hdc, this, _yOffset);
            _highlightPainter = new ViewMapPainter(NativeHandle, hdc, this, _yOffset);
            _screenPainter = new ViewMapPainter(NativeHandle, hdc, this, _yOffset);

            //Yes you should release a DC that came from the cache
            User32.ReleaseDC(NativeHandle, hdc);
        }

        #region ComputeRegions

        private unsafe void ComputeRegions()
        {
            //For each section, assign proportional widths. We want each section to be at least 3 pixels. Originally I wanted a border around
            //each section, but that didn't work; you can't see where the borders are once everything is coloured in!

            using var scope = App.AcquireFileAccessor();

            if (scope.FileAccessor == null)
                return;

            var width = Width;

            if (width == 0)
                return;

            var fileAccessor = scope.FileAccessor;

            var totalBytes = fileAccessor.Length;
            var sectionAccessors = fileAccessor.SectionAccessors;

            Debug.Assert(totalBytes != 0); //FileAccessor must set this in its ctor

            var visualSections = new VisualSection[sectionAccessors.Length];

            var start = 0;

            //We start by reserving 3 at least 3 pixels for each section (empty sections are excluded)
            var numVisibleSections = GetNumVisibleSections(sectionAccessors);
            var reservedWidth = MIN_WIDTH * sectionAccessors.Length;

            //Now, for each section, distribute the remaining pixels based on the size of each section

            var virtualWidth = width;

            var availableWidth = virtualWidth - reservedWidth;

            //A big PDB like msedge.dll hits this
            if (availableWidth < 0)
                throw new NotImplementedException("Don't know how to handle having so many sections that we can't accomodate the required minimum section widths");

            var totalWidth = AssignVisualWidth(sectionAccessors, visualSections, totalBytes, availableWidth);

            Debug.Assert(totalWidth <= virtualWidth);

            //If we didn't allocate all of the pixels, we should sort the sections by their lengths and distribute the pixels to the largest sections
            if (totalWidth < virtualWidth)
            {
                var missing = virtualWidth - totalWidth;

                //Each section could introduce at most a 1 pixel rounding error, so the number of missing pixels should be less than the number of sections
                Debug.Assert(missing < sectionAccessors.Length);

                {
                    for (var i = 0; i < sectionAccessors.Length; i++)
                    {
                        ref var sectionAccessor = ref sectionAccessors[i];

                        indices[i] = i;
                        lengths[i] = sectionAccessor.Length;
                    }

                    Array.Sort(lengths, indices, 0, sectionAccessors.Length);

                    for (var i = 0; i < missing; i++)
                    {
                        totalWidth++;

                        ref var visualSection = ref visualSections[indices[sectionAccessors.Length - i - 1]];

                        visualSection.Width++;
                    }

                    Debug.Assert(totalWidth == virtualWidth);
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(indices);
                    ArrayPool<int>.Shared.Return(lengths);
                }
            }

            //Now that we know how big each section is going to be, figure out which addresses each pixel should represent

            var totalPhysicalWidthUsed = 0;

            DirectoryInfo[]? dataDirectories = null;
            int nextDataDirectoryIndex = 0;
            using var directories = new ValueList<(int startPixel, int endPixel, int directoryIndex)>();
            for (var i = 0; i < visualSections.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                ref var visualSection = ref visualSections[i];

                if (sectionAccessor.IsEmpty || visualSection.Width == 0)
                {
                    if (i == 0)
                    {
                        visualSection.PhysicalStartPixel = -1;
                        visualSection.Width = 0;
                        continue;
                    }

                    //Make the visual section empty with a start and end address the same as the previous section
                    Debug.Assert(i > 0);
                    ref var previous = ref visualSections[i - 1];
                    visualSection.PhysicalStartPixel = start;
                    visualSection.Width = 0;
                    continue;
                }

                visualSection.PhysicalStartPixel = start;

                var effectiveWidth = visualSection.Width;
                Debug.Assert(effectiveWidth >= 0);

                //bytesPerPixel is relative to the width of _this_ region. So if there's 2 pixels,
                //the two pixels at the start and the halfway point
                var bytesPerPixel = (int) ((sectionAccessor.Length / effectiveWidth) / _scale);

                if (bytesPerPixel == 0)
                {
                    //We're so zoomed in we want to show more pixels than we have bytes! Clamp to our actual width
                    effectiveWidth = sectionAccessor.Length;
                    visualSection.Width = effectiveWidth;
                    _isMaxScroll = true;

                    bytesPerPixel = 1;
                }

                totalPhysicalWidthUsed += effectiveWidth;

                if (totalPhysicalWidthUsed > width)
                {
                    var diff = totalPhysicalWidthUsed - width;
                    effectiveWidth -= diff;
                    Debug.Assert(effectiveWidth > 0);
                    visualSection.Width -= diff;
                var data = new (long startAddress, long pixelAddress, IntPtr info)[effectiveWidth];

                Debug.Assert(bytesPerPixel > 0);

                for (var j = 0; j < effectiveWidth; j++)
                {
                    //For each pixel, get the previous non-body value to it
                    var byteIndex = (j + visualSection.LogicalStartPixel) * bytesPerPixel;

                    var offset = byteIndex;
                    var pixelAddress = sectionAccessor.StartAddress + offset;

                    var pViewByte = sectionAccessor.pViewBytes + byteIndex;
                    var startAddress = sectionAccessor.StartAddress + offset;

                    //With PDB files we have to watch out, because a value might be split across multiple pages,
                    //and the value at the start of a given page might be the body
                    while (pViewByte->Kind == ViewByteKind.Body)
                    {
                        if (pViewByte->BodyKind == ViewByteBodyKind.SplitHead)
                        {
                            /* We store two values: the address of the pixel, and the address of the head.
                             * The goal in this loop is to get the address of the head, and a ViewByte that
                             * describes the head's entity. We've hit the beginning of a disconnected page however,
                             * so in order to rewind further we need to ask the view accessor to trace through the
                             * previous page for us. This is not an issue for the ViewMap; as stated, the pixel address
                             * stores the fact the value is in a disconnected page. We just need to obtain the head info
                             * so we know what name and color to apply, etc */

                            ((PDBFileAccessor) fileAccessor).GetSplitHeadOrigin(ref pViewByte, ref startAddress, out _, out _);
                            break;
                        }

                        pViewByte--;
                        startAddress--;
                    }

                    //We just need to store the address of the target value;
                    //the type of value contained at this address can be computed during painting,
                    //and its name can be looked up on hover.
                    data[j] = (startAddress, pixelAddress, (IntPtr) pViewByte);
                }

                visualSection.Data = data;

                Debug.Assert(dataDirectories == null || fileAccessor is not PDBFileAccessor); //The logic below uses the start address which could be somewhere totally different in a PDBFile duue to split pages

                //Associate any data directories with this visual section
                while (dataDirectories != null && nextDataDirectoryIndex < dataDirectories.Length)
                {
                    var nextDataDirectory = dataDirectories[nextDataDirectoryIndex];

                    var firstPixelAddress = data[0].startAddress;
                    var lastPixelAddress = data[data.Length - 1].startAddress;

                    if (nextDataDirectory.Start >= firstPixelAddress && nextDataDirectory.Start <= lastPixelAddress)
                    {
                        var startIndex = visualSection.GetBestPixel(nextDataDirectory.Start, 0, data.Length - 1);
                        Debug.Assert(startIndex != -1);

                        var endIndex = visualSection.GetBestPixel(nextDataDirectory.End, startIndex, data.Length - 1);
                        directories.Add((startIndex, endIndex, nextDataDirectoryIndex));

                        nextDataDirectoryIndex++;
                    }
                    else
                        break;
                }

                if (directories.Count > 0)
                {
                    visualSection.Directories = directories.ToArray();
                    directories.Clear();
                }

                start += visualSection.Width;
            }

            _visualSections = visualSections;
        }

        private static int GetNumVisibleSections(SectionAccessor[] sectionAccessors)
        {
            var numVisibleSections = 0;

            for (var i = 0; i < sectionAccessors.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                if (!sectionAccessor.IsEmpty)
                    numVisibleSections++;
            }

            return numVisibleSections;
        }

        private int AssignVisualWidth(
            SectionAccessor[] sectionAccessors,
            VisualSection[] visualSections,
            long totalBytes,
            int availableVirtualWidth)
        {
            var totalWidth = 0;

            for (var i = 0; i < visualSections.Length; i++)
            {
                ref var sectionAccessor = ref sectionAccessors[i];

                if (sectionAccessor.IsEmpty)
                    continue;

                ref var visualSection = ref visualSections[i];

                var proportion = (double) sectionAccessor.Length / totalBytes;
                var proportionalWidth = (int) Math.Floor(availableVirtualWidth * proportion);

                var actualWidth = MIN_WIDTH + proportionalWidth;

                totalWidth += actualWidth;

                Debug.Assert(actualWidth > 0);
                visualSection.Width = actualWidth;
            }

            return totalWidth;
        }

        #endregion

        protected override void OnPaint(HDC hdc)
        {
            using var scope = App.AcquireFileAccessor();

            if (_visualSections == null || scope.FileAccessor == null)
            {
                //Nothing for us to do here, so just paint the background straight to the screen
                User32.FillRect(hdc, ClientRectangle, DefaultBackgroundBrush);
                return;
            }

            if (!_basePainter.IsValid)
                _basePainter.RenderBaseLayer(scope.FileAccessor);

            if (_highlightedSection != -1)
            {
                if (!_highlightPainter.IsValid)
                {
                    _basePainter.CopyTo(_highlightPainter);
                    _highlightPainter.DrawHighlights(scope.FileAccessor);
                }

                _highlightPainter.CopyTo(_screenPainter);
            }
            else
            {
                _basePainter.CopyTo(_screenPainter);
            }

            _screenPainter.DrawArrow(_arrowXPos);

            _screenPainter.CopyTo(hdc);
        }

        protected override void OnWindowPosChanged()
        {
            base.OnWindowPosChanged();

            var hdc = User32.GetDCEx(NativeHandle, default, GET_DCX_FLAGS.DCX_CACHE);

            var clientRect = ClientRectangle;

            _basePainter.Resize(hdc, clientRect);
            _highlightPainter.Resize(hdc, clientRect);
            _screenPainter.Resize(hdc, clientRect);

            User32.ReleaseDC(NativeHandle, hdc);

            ComputeRegions();

            //Windows won't repaint everything properly unless you call Invalidate. WinForms automatically calls
            //Invalidate in response to DoLayout
            Invalidate();
        }
        protected override void OnMouseDown(int x, int y)
        {
            OnMouseMove(x, y);
        }

        protected override void OnMouseUp(int x, int y)
        {
            _wasLeftMouseDown = false;
        }

        //In WinForms we get a mouse move even when clicking; I haven't figured out why, so we'll just relay to this
        //on mouse down
        protected override unsafe void OnMouseMove(int x, int y)
        {
            using var scope = App.AcquireFileAccessor();

            if (scope.FileAccessor == null)
                return;

            /* When the tooltip is shown, another mouse move event will be triggered, which will cause us to show the tooltip again,
             * etc. Thus, we need to check if we're at the same point that we showed the tooltip, and if so bail out
             * 
             * You get MouseMove events as long as the mouse button is pressed (perhaos as a result of the various things we to do hook/capture
             * the mouse). If you pressed the button and then moved onto this element, you won't get any events */

            var visualSections = _visualSections;

            if (visualSections == null)
                return;

            //If the high order bit is 1, the key is down (i.e. 0x80000000)
            var isLeftMouseDown = User32.GetKeyState((int) VIRTUAL_KEY.VK_LBUTTON) < 0;

            //If we're holding down the left mouse button and dragging the mouse, even if we're outside the canvas, we still want to allow dragging
            if ((y < _yOffset || y > (COLORLINE_HEIGHT + _yOffset)))
            {
                if (isLeftMouseDown && !_wasLeftMouseDown)
                {
                    //We just clicked for the first time and we were out of bounds. Ignore
                    return;
                }
                else if (!isLeftMouseDown)
                {
                    //Left mouse button has been released, and we've moved out of the canvas
                    OnMouseLeave();
                    return;
                }
            }

            _wasLeftMouseDown = isLeftMouseDown;

            //If you click and drag all the way to the left, you can get a negative X coordinate

            if (x < 0)
                x = 0;

            if (x >= Width)
                x = Width - 1;

            if (!isLeftMouseDown)
            {
                if (_lastToolTipPos == x)
                {
                    //We can fast path out of this. We don't need to move the arrow, and implicitly the same section
                    //is still highlighted
                    return;
                }
            }

            //Binary search to find which section we're in

            var sectionIndex = GetVisualSectionUnderCursor(x);

            ref var visualSection = ref visualSections[sectionIndex];

            //This is the section
            var offset = x - visualSection.PhysicalStartPixel;
            Debug.Assert(offset < visualSection.Width);
            var pViewByte = (ViewByte*) item.pViewByte;

            if (_lastToolTipPos != x)
            {
                var builder = new ValueStringBuilder();

                try
                {
                    GetTooltipText(scope.FileAccessor, pViewByte, sectionIndex, item.startAddress, item.pixelAddress, ref builder);

                    const int limit = 150;

                    if (builder.Length > limit)
                    {
                        builder.Length = limit;
                        builder.Append("...");
                    }

                    builder.Append('\0');

                    _tooltip.SetToolTip(Window, builder.AsSpan());
                    _lastToolTipPos = x;
                }
                finally
                {
                    builder.Dispose();
                }
            }

            var needInvalidate = false;

            var oldHighlightedSection = _highlightedSection;

            if (_highlightedSection == sectionIndex)
            {
                //This section is already highlighted
            }
            else
            {
                //We need to re-render everything using the highlighted colors
                _highlightedSection = sectionIndex;
                _highlightPainter.Invalidate();

                needInvalidate = true;
            }
                if (_highlightedSection == _arrowSectionIndex)
                {
                    //We're still in the same section as before. Which direction are we moving?
                    if (item.pixelAddress < _arrowAddress)
                    {
                        //We're dragging to the left
                        _arrowXPos = visualSection.PhysicalStartPixel + visualSection.GetBestPixel(item.pixelAddress, 0, _arrowXPos - visualSection.PhysicalStartPixel);
                        _arrowSectionIndex = sectionIndex;
                    }
                    else
                    {
                        //We're dragging to the right
                        _arrowXPos = visualSection.PhysicalStartPixel + visualSection.GetBestPixel(item.pixelAddress, _arrowXPos - visualSection.PhysicalStartPixel, visualSection.Data.Length - 1);
                        _arrowSectionIndex = sectionIndex;
                    }
                }
                else
                {
                    //It's a different section. Binary search all pixels to find the best match

                    _arrowXPos = visualSection.PhysicalStartPixel + visualSection.GetBestPixel(item.pixelAddress, 0, visualSection.Data.Length - 1);
                    _arrowSectionIndex = sectionIndex;
                }

                _arrowAddress = item.pixelAddress;
            if (needInvalidate)
            {
                //The key to fast painting is to paint what you want _immediately_. Calling Invalidate() and waiting for a WM_PAINT is too slow, as Windows seems to allow these requests to build
                //up before actually dispatching the WM_PAINT (either that, or the the process of doing the dispatch is also slow)
                var hdc = User32.GetDCEx(NativeHandle, default, GET_DCX_FLAGS.DCX_CACHE);

                OnPaint(hdc);

                User32.ReleaseDC(NativeHandle, hdc);
            }
        }

        private unsafe void GetTooltipText(
            FileAccessor fileAccessor,
            ViewByte* pViewByte,
            int sectionAccessorIndex,
            long itemStartAddress,
            long itemPixelAddress,
            ref ValueStringBuilder builder)
        {
            Debug.Assert(pViewByte->Kind != ViewByteKind.Body); //Need special logic for split head

            ref var sectionAccessor = ref fileAccessor.SectionAccessors[sectionAccessorIndex];

            //Don't make the entity have to do gymnastics rewinding or anything; tell it what the start is
            //and then hack it to model the actual displacement that we're interested in
            var entity = fileAccessor.GetEntity(itemStartAddress, pViewByte, sectionAccessorIndex);
            entity.TargetAddress = itemPixelAddress;
            entity.Displacement = itemPixelAddress - itemStartAddress;

            builder.Append(sectionAccessor.Name);
            builder.Append(":");
            builder.AppendHex((uint) itemPixelAddress);
            builder.Append(' ');

            entity.ToString(ref builder);
        }

        protected override void OnMouseLeave()
        {
            if (_lastToolTipPos != -1)
                _lastToolTipPos = -1;
                _tooltip.SetToolTip(Window, null);
            }

            if (_highlightedSection != -1)
            {
                _highlightedSection = -1;
                _highlightPainter.Invalidate();
                Invalidate();
            }
        }

        private int GetVisualSectionUnderCursor(int xPos)
        {
            var low = 0;
            var high = _visualSections!.Length - 1;

            while (low <= high)
            {
                var mid = (low + high) / 2;
                ref var visualSection = ref _visualSections[mid];

                if (xPos < visualSection.PhysicalStartPixel)
                    high = mid - 1;
                else if (xPos >= visualSection.PhysicalStartPixel + visualSection.Width)
                    low = mid + 1;
                else
                    return mid;
            }
        }
    }
}
