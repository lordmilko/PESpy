using System;
using System.Diagnostics;

namespace PESpy
{
    public class LogicalLine
    {
        public int Depth;

        public int LineHeight;
        public int Top;
        public PhysicalLine[] Lines;
        public ViewByteFormatRange[] FormatRanges;

        public PhysicalLine FirstVisibleLine => Lines[FirstVisibleLineIndex];

        public int FirstVisibleLineIndex
        {
            get
            {
                for (var i = 0; i < Lines.Length; i++)
                {
                    if (Lines[i].IsVisible)
                        return i;
                }

                throw new NotImplementedException();
            }
        }

        public PhysicalLine LastVisibleLine => Lines[LastVisibleLineIndex];

        internal int LastVisibleLineIndex
        {
            get
            {
                for (var i = Lines.Length - 1; i >= 0; i--)
                {
                    var line = Lines[i];

                    if (line.IsVisible)
                        return i;
                }

                throw new NotImplementedException();
            }
        }

        public string Text;

        internal int NumVisibleLines
        {
            get
            {
                var count = 0;

                foreach (var line in Lines)
                {
                    if (line.IsVisible)
                        count++;
                }

                return count;
            }
        }

        public LogicalLine(string text, int depth, PhysicalLine[] lines, ViewByteFormatRange[] formatRanges)
        {
            Debug.Assert(depth > 0);

            Text = text;
            Depth = depth;
            Lines = lines;
            FormatRanges = formatRanges;

            foreach (var line in lines)
                line.LogicalLine = this;
        }

        internal ReadOnlySpan<char> GetText(int start, int length) => Text.AsSpan(start, length);

        internal ReadOnlySpan<ViewByteFormatRange> GetRanges(int start, int length) => FormatRanges.AsSpan(start, length);

        public override string ToString()
        {
            return Text;
        }
    }
}
