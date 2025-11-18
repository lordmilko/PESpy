using System;
using System.Collections.Generic;
using System.Diagnostics;
using PInvoke;

namespace PESpy.Tests
{
    internal class MockGraphics : IGraphics
    {
        public int LineHeight => 20;

        public HDC MemDC => default;

        internal List<MockTextLine> MemoryLines { get; } = new List<MockTextLine>();

        internal MockTextLine[] ScreenLines { get; private set; }

        private int _height;

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void CreateMemoryBitmap(int width, int height)
        {
            _height = height;
        }

        public void FillBackground(in RECT rect)
        {
            //All lines that overlap with the rectangle should be removed.
            //The rectangle should fully cover a given line; assert that there's no partial
            //overlaps
            if (MemoryLines.Count == 0)
                return; //Nothing to do

            //The caller calls ScrollMemDC which causes lines up the top to be scrolled off screen,
            //and the lines below to be shifted up. However, the pixels of those lines are still
            //visible on screen, so FillBackground will then be called to clear those pixels out.
            //We would not expect to have to modify any of our bookkeeping in that situation. However,
            //if we have a Goto command, then the whole canvas needs to be repainted

            var toRemove = new List<MockTextLine>();

            foreach (var line in MemoryLines)
            {
                if (line.Intersects(rect))
                {
                    if (!line.IsEnclosedBy(rect))
                        throw new NotImplementedException();

                    toRemove.Add(line);
                }
            }

            MemoryLines.RemoveAll(v => toRemove.Contains(v));
        }

        public void DrawText(ReadOnlySpan<char> str, int left, int top, ref int right, int fontHeight)
        {
            var text = str.ToString();

            right = text.Length;

            var newLine = new MockTextLine(text, left, top, right, fontHeight);

            for (var i = 0; i < MemoryLines.Count; i++)
            {
                var existingLine = MemoryLines[i];

                if (top < existingLine.Top)
                {
                    MemoryLines.Insert(i, newLine);
                    return;
                }

                if (existingLine.Top == top)
                {
                    if (right == 0)
                    {
                        //We're drawing over an existing in-memory line
                        MemoryLines[i] = newLine;
                    }
                    else
                    {
                        //We're appending to an existing line
                        MemoryLines[i].MergeWith(newLine);
                    }

                    return;
                }
            }

            //Doesn't overlap; add the line
            MemoryLines.Add(newLine);
        }

        public void ScrollMemDC(int destX, int destY, int width, int height, int sourceX, int sourceY)
        {
            //Subtract sourceY from all lines, and then remove any whose top is negative

            foreach (var line in MemoryLines)
                line.Top -= sourceY;

            MemoryLines.RemoveAll(l => l.Top < 0 || l.Top >= _height);
        }

        public int GetVerticalScrollThumb()
        {
            throw new NotImplementedException();
        }

        public void SetScrollInfo(SCROLLBAR_CONSTANTS scrollBar, in SCROLLINFO scrollInfo)
        {
        }

        public void CommitMemDC(int destX, int destY, int width, int height, int sourceX, int sourceY, HDC destinationDC = default)
        {
            //Copy all lines within the specified range from memory to screen

            //As long as the source and destination are both 0 we don't need to translate anything
            Debug.Assert(destY == 0);
            Debug.Assert(sourceY == 0);

            ScreenLines = MemoryLines.ToArray();
        }

        public void Dispose()
        {
        }
    }
}
