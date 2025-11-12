using System;
using System.Diagnostics;
using Iced.Intel;
using PESpy.View;
using PInvoke;

namespace PESpy
{
    public class TextViewRenderer : ISymbolResolver, IDisposable
    {
        public LogicalLineCollection LogicalLines => _lines;

        private LogicalLineCollection _lines;
        private ViewByteFormatter _formatter;

        private int _hBitmapHeight;
        private int _gotoAddress;
        private int _lastThumbPosition;
        internal int _yTop;

        private int _width;
        private int _height;

        private readonly IGraphics _graphics;
        private readonly FileAccessor _fileAccessor;

        public TextViewRenderer(IGraphics graphics, FileAccessor fileAccessor, int width, int height, int yTop)
        {
            Debug.Assert(graphics != null);
            Debug.Assert(fileAccessor != null);

            _graphics = graphics;
            _fileAccessor = fileAccessor;

            _lines = new LogicalLineCollection(20);
            _formatter = new ViewByteFormatter(fileAccessor, this);

            _yTop = yTop;
            _width = width;
            _height = height;

            //Ensure the bitmap is initialized
            Resize(width, height);
        }

        public void Goto(int targetAddress, bool isScrolling)
        {
            if (_lastThumbPosition == targetAddress)
                return;

            /* If there is a large area of padding, you could have a single line at the top of the view
             * that represents a large number of bytes. When we drag the scrollbar down, we need to skip
             * over all of the bytes represented within that line, and snap to the next item after it.
             * Conversely, if we're scrolling up, if prior to the current item is a large number of padding
             * bytes, we need to snap to the start of the padding */

            var numVisibleLinesDouble = (double) (_height - _yTop) / _graphics.LineHeight;
            var numVisibleLines = (int) Math.Ceiling(numVisibleLinesDouble);

            _formatter.ClearPath();
            _lines.Clear();

            _formatter.StartWithoutOwner(targetAddress, numVisibleLines);
            var rect = new RECT(0, 0, _width, _hBitmapHeight);

            _graphics.FillBackground(rect);

            PaintBelow(default, numVisibleLinesDouble);
        }

        public void Paint(HDC hdc)
        {
            var rect = new RECT(0, 0, _width, _hBitmapHeight);

            _graphics.FillBackground(rect);

            var numVisibleLinesDouble = (double) (_height - _yTop) / _graphics.LineHeight;
            _formatter.StartWithoutOwner(_gotoAddress, (int) Math.Ceiling(numVisibleLinesDouble));

            PaintBelow(hdc, numVisibleLinesDouble);
        }

        private void PaintBelow(HDC hdc, double numVisibleLinesDouble)
        {
            var formatter = _formatter;

            var logicalLines = formatter.PeekLogicalLines();
            _lines.Append(logicalLines);
            var yPos = 0;
            int right = 0;

            var numLinesRemaining = (int) Math.Ceiling(numVisibleLinesDouble);

            DrawMultiLineText(logicalLines, 0, ref yPos, ref right, ref numLinesRemaining, _graphics.LineHeight);

            formatter.ClearLogicalLines();

            while (numLinesRemaining > 0)
            {
                if (!formatter.MoveNext())
                    break;

                logicalLines = formatter.PeekLogicalLines();
                _lines.Append(logicalLines);

                DrawMultiLineText(logicalLines, 0, ref yPos, ref right, ref numLinesRemaining, _graphics.LineHeight);

                formatter.ClearLogicalLines();
            }

            _graphics.CommitMemDC(0, _yTop, _width, _height, 0, 0, hdc);

            UpdateScrollBar();
        }

        public void ScrollLinesDown(int numLinesToScroll)
        {
            var numLinesRemaining = numLinesToScroll;

            var yPos = ShiftPixelsUp(numLinesRemaining);

            var formatter = _formatter;

            if (!_lines.LastPhysicalLine.IsVisible)
            {
                //Before generating more rows, if the bottom logical line is partially visible,
                //we should use the text we've already generated

                var lastLogicalLine = _lines.LastLogicalLine;

                foreach (var line in lastLogicalLine.Lines)
                {
                    if (line.IsVisible)
                        continue;

                    var right = 0;
                    DrawSingleLineDown(line, ref yPos, ref right, ref numLinesRemaining, _graphics.LineHeight);

                    if (numLinesRemaining == 0)
                        break;
                }
            }

            if (numLinesRemaining == 0)
                goto end;

            if (formatter.Direction == Direction.Up)
            {
                //See the comments in ScrollLinesUp. We need to reverse the child index we're "up to" in the current path to account
                //for who is currently at the _bottom_ of the screen, not who is currently at the _top_
                Debug.Assert(_lines.Count > 0);

                //Also sets the direction
                if (formatter.PrepareToFastForward(_lines.LastLogicalLine, numLinesRemaining))
                {
                    //We had no idea what was going on, and had to do StartWithoutOwner
                    WriteLinesDown(formatter, ref yPos, ref numLinesRemaining);
                }
            }
            //Write all new lines to the memory DC

            //Ask the formatter to MoveNext

            while (numLinesRemaining > 0)
            {
                if (!formatter.MoveNext())
                    break;

                WriteLinesDown(formatter, ref yPos, ref numLinesRemaining);
            }

            if (numLinesRemaining > 0)
                throw new NotImplementedException(); //we reached the bottom. we might have over-scrolled the memory dc, so now we need to undo that? but the pixels might have already been lost? and what if the window was tiny to begin with? maybe dont draw anything until we've got the new lines to draw, then we know how much to shift by

end:
            //Now blit it to the screen
            _graphics.CommitMemDC(0, _yTop, _width, _height, 0, 0);

            UpdateScrollBar();
        }

        private void WriteLinesDown(ViewByteFormatter formatter, ref int yPos, ref int numLinesRemaining)
        {
            var logicalLines = formatter.PeekLogicalLines();
            _lines.Append(logicalLines);

            var right = 0;
            DrawMultiLineText(logicalLines, 0, ref yPos, ref right, ref numLinesRemaining, _graphics.LineHeight);

            formatter.ClearLogicalLines();
        }

        //For scrolling down
        private int ShiftPixelsUp(int numLinesToScroll)
        {
            //Scroll the entire memory DC up (which only shows complete lines)
            var scrollAmount = numLinesToScroll * _graphics.LineHeight;

            var hMemDC = _graphics.MemDC;

            _graphics.ScrollMemDC(destX: 0, destY: 0, width: _width, height: _hBitmapHeight, sourceX: 0, sourceY: scrollAmount);

            //Remove lines from the start
            _lines.RemoveLeadingLines(numLinesToScroll);

            var yPos = _hBitmapHeight - scrollAmount;

            //White out the area we just scrolled at the bottom of the screen
            var rect = new RECT(0, yPos, _width, _hBitmapHeight);
            _graphics.FillBackground(rect);

            return yPos;
        }

        public void ScrollLinesUp(int numLinesToScroll)
        {
            var numLinesRemaining = numLinesToScroll;

            var firstPhysicalLine = _lines.FirstPhysicalLine;

            var yPos = ShiftPixelsDown(numLinesToScroll);

            if (!firstPhysicalLine.IsVisible)
            {
                //Before generating more rows, if the top logical line is partially visible,
                //we should use the text we've already generated

                var firstLogicalLine = _lines.FirstLogicalLine;

                for (var i = firstLogicalLine.Lines.Length - 1; i >= 0; i--)
                {
                    var line = firstLogicalLine.Lines[i];

                    if (line.IsVisible)
                        continue;

                    var right = 0;
                    DrawSingleLineUp(line, ref yPos, ref right, ref numLinesRemaining, _graphics.LineHeight);

                    if (numLinesRemaining == 0)
                        break;
                }
            }

            if (numLinesRemaining == 0)
                goto end;

            //Can't use being on address 0 as meaning we're already at the top; the first field
            //in the first struct is also at address 0, but there's a struct header before us!

            var formatter = _formatter;

            if (formatter.Direction == Direction.Down)
            {
                //If we were previously scrolling down, our path may say that our 6th field was the last field written. But we're about to scroll up,
                //which means we need to reverse course. We don't care what field is currently at the bottom of the screen, we care about what field
                //is at the _top_ of the screen
                Debug.Assert(_lines.Count > 0);

                //Also sets the direction. We only request 1 line here because we're going backwards, we don't want to generate forwards lines
                if (formatter.PrepareToRewind(_lines.FirstLogicalLine, 1))
                {
                    //If we had no idea what's going on, we had to call StartWithoutOwner and now we've just written the first line
                    WriteLinesUp(formatter, ref yPos, ref numLinesRemaining);
                }
            }

            while (numLinesRemaining > 0)
            {
                if (!formatter.MovePrevious())
                    throw new NotImplementedException();

                WriteLinesUp(formatter, ref yPos, ref numLinesRemaining);
            }
            end:
            //Now blit it to the screen
            _graphics.CommitMemDC(0, _yTop, _width, _height, 0, 0);

            UpdateScrollBar();
        }

        private void WriteLinesUp(ViewByteFormatter formatter, ref int yPos, ref int numLinesRemaining)
        {
            var logicalLines = formatter.PeekLogicalLines();
            _lines.Prepend(logicalLines);

            //This draws from the last line to the first, decrementing yPos prior to writing each line
            var right = 0;
            DrawReverseMultiLineText(logicalLines, 0, ref yPos, ref right, ref numLinesRemaining, _graphics.LineHeight);

            formatter.ClearLogicalLines();
        }

        //For scrolling up
        private int ShiftPixelsDown(int numLinesToScroll)
        {
            //Scroll the entire memory DC down (which only shows complete lines)
            var scrollAmount = numLinesToScroll * _graphics.LineHeight;

            var hMemDC = _graphics.MemDC;

            _graphics.ScrollMemDC(destX: 0, destY: 0, width: _width, height: _hBitmapHeight, sourceX: 0, sourceY: -scrollAmount);

            //Remove lines from the end
            _lines.RemoveTrailingLines(numLinesToScroll);

            var yPos = scrollAmount;

            //White out the area we just scrolled at the top of the screen
            var rect = new RECT(0, 0, _width, yPos);
            _graphics.FillBackground(rect);

            return yPos;
        }

        private void DrawReverseMultiLineText(
            Span<LogicalLine> logicalLines,
            int left,
            ref int yPos,
            ref int right,
            ref int numLinesRemaining,
            int lineHeight)
        {
            Debug.Assert(numLinesRemaining > 0);

            for (var i = logicalLines.Length - 1; i >= 0; i--)
            {
                var logicalLine = logicalLines[i];

                for (var j = logicalLine.Lines.Length - 1; j >= 0; j--)
                {
                    var physicalLine = logicalLine.Lines[j];

                    DrawSingleLineUp(physicalLine, ref yPos, ref right, ref numLinesRemaining, lineHeight);

                    if (numLinesRemaining == 0)
                    {
                        //We had better be on the first logical line; we should not have written more logical lines than are necessary
                        Debug.Assert(i == 0);
                        return;
                    }
                }
            }
        }

        private void DrawSingleLineUp(
            PhysicalLine physicalLine,
            ref int yPos,
            ref int right,
            ref int numLinesRemaining,
            int lineHeight)
        {
            yPos -= lineHeight;

            right = 0;
            _graphics.DrawText(physicalLine.Text, 0, yPos, ref right, lineHeight);

            physicalLine.IsVisible = true;

            numLinesRemaining--;
        }

        //For scrolling down
        private unsafe void DrawMultiLineText(
            Span<LogicalLine> logicalLines,
            int left,
            ref int yPos,
            ref int right,
            ref int numLinesRemaining,
            int lineHeight)
        {
            Debug.Assert(numLinesRemaining > 0);

            for (var i = 0; i < logicalLines.Length; i++)
            {
                var logicalLine = logicalLines[i];

                foreach (var physicalLine in logicalLine.Lines)
                {
                    DrawSingleLineDown(physicalLine, ref yPos, ref right, ref numLinesRemaining, lineHeight);

                    if (numLinesRemaining == 0)
                    {
                        //We had better be on the last logical line; we should not have written more logical lines than are necessary
                        Debug.Assert(i == logicalLines.Length - 1);
                        return;
                    }
                }
            }
        }

        private void DrawSingleLineDown(
            PhysicalLine physicalLine,
            ref int yPos,
            ref int right,
            ref int numLinesRemaining,
            int lineHeight)
        {
            right = 0;
            _graphics.DrawText(physicalLine.Text, 0, yPos, ref right, lineHeight);

            yPos += lineHeight;
            physicalLine.IsVisible = true;

            numLinesRemaining--;
        }

        internal unsafe void UpdateScrollBar()
        {
            int pageSize;
            int position = 0;

            if (_lines.Count > 0)
            {
                var firstLine = _lines.FirstVisibleLine;
                var lastLine = _lines.LastVisibleLine;

                position = firstLine.StartAddress;

                if (lastLine.Length == 0)
                {
                    var found = false;

                    //The last line is a header. That kind of messes up our calculations. Fallback to the last line that does have an end offset
                    for (var i = _lines.Count - 1; i >= 0; i--)
                    {
                        var logicalLine = _lines[i];

                        for (var j = logicalLine.Lines.Length - 1; j >= 0; j--)
                        {
                            var candidateLine = logicalLine.Lines[j];

                            if (candidateLine.Length != 0)
                            {
                                lastLine = candidateLine;
                                found = true;
                                break;
                            }
                        }

                        if (found)
                            break;
                    }
            Debug.Assert(pageSize > 0);

            var lpsi = new SCROLLINFO
            {
                cbSize = sizeof(SCROLLINFO),
                fMask = SCROLLINFO_MASK.SIF_ALL,
                nMin = 0,
                nMax = _fileAccessor.Length,
                nPage = pageSize,
                nPos = position
            };

            if (_lastThumbPosition != position)
                App.RaisePositionChanged(this, position);

            _lastThumbPosition = position;

            //Showing the scrollbar means that the client area will be resized, which means we're going to nuke our bitmap
            _graphics.SetScrollInfo(SCROLLBAR_CONSTANTS.SB_VERT, lpsi);
        }

        public void Resize(int width, int height)
        {
            _width = width;
            _height = height;

            var numLines = Math.Ceiling((double) (height - _yTop) / _graphics.LineHeight);

            var newBitmapHeight = (int) (numLines * _graphics.LineHeight);

            if (newBitmapHeight == _hBitmapHeight)
                return; //No change, nothing to do

            _hBitmapHeight = newBitmapHeight;

            _graphics.CreateMemoryBitmap(width, newBitmapHeight);
        }

        #region ISymbolResolver

        bool ISymbolResolver.TryGetSymbol(in Instruction instruction, int operand, int instructionOperand, ulong address, int addressSize, out SymbolResult symbol)
        {
            var kind = instruction.GetOpKind(operand);

            switch (kind)
            {
                case OpKind.Register:
                case OpKind.Immediate8:
                case OpKind.Immediate8_2nd:
                case OpKind.Immediate16:
                case OpKind.Immediate32:
                case OpKind.Immediate64:
                case OpKind.Immediate8to16:
                case OpKind.Immediate8to32:
                case OpKind.Immediate8to64:
                case OpKind.Immediate32to64:
                    symbol = default;
                    return false;

                case OpKind.NearBranch16:
                case OpKind.NearBranch32:
                case OpKind.NearBranch64:
                    return TryGetDataSymbol(address, (int) instruction.NearBranch64, out symbol);

                case OpKind.FarBranch16:
                case OpKind.FarBranch32:
                    return TryGetDataSymbol(address, (int) instruction.FarBranch32, out symbol);

                case OpKind.Memory:
                    switch (instruction.MemoryBase)
                    {
                        case Register.EIP:
                        case Register.RIP:
                            return TryGetDataSymbol(address, (int) instruction.MemoryDisplacement64, out symbol);

                        case Register.None:
                            Debug.Assert(instruction.MemorySegment != Register.None);

                            //I haven't seen this on x16 or x64
                            if (instruction.CodeSize == CodeSize.Code32)
                            {
                                Debug.Assert(_fileAccessor is PEFileAccessor);
                                return TryGetDataSymbol(address, (int) (instruction.MemoryDisplacement64 - (ulong) _fileAccessor.ImageBase), out symbol);
                            }
        private bool TryGetDataSymbol(ulong address, int rva, out SymbolResult symbol)
        {
            if (_fileAccessor.TryGetDataSymbol(address, rva, out var name, out var displacement))
            {
                symbol = new SymbolResult(address - (uint) displacement, name.ToString());
                return true;
            }

            symbol = default;
            return false;
        }

        #endregion
}
