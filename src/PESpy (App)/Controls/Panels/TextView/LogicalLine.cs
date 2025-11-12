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

        public PhysicalLine FirstVisibleLine
        {
            get
            {
                foreach (var line in Lines)
                {
                    if (line.IsVisible)
                        return line;
                }

                throw new NotImplementedException();
            }
        }

        internal PhysicalLine LastVisibleLine
        {
            get
            {
                for (var i = Lines.Length - 1; i >= 0; i--)
                {
                    var line = Lines[i];

                    if (line.IsVisible)
                        return line;
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

        public LogicalLine(string text, int depth, PhysicalLine[] lines)
        {
            Debug.Assert(depth > 0);

            Text = text;
            Depth = depth;
            Lines = lines;

            foreach (var line in lines)
                line.LogicalLine = this;
        }

        internal ReadOnlySpan<char> GetText(int start, int length) => Text.AsSpan(start, length);

        public override string ToString()
        {
            return Text;
        }
    }
}
