using System;

namespace PESpy
{
    public class PhysicalLine
    {
        public int StartAddress;
        public int Length;
        public int EndAddress => StartAddress + Length;

        public bool IsVisible;

        public int RelativeIndex;
        public int Top => (LogicalLine.Top - (RelativeIndex * LogicalLine.LineHeight)); //May be off screen

        public readonly int StartIndex;
        public readonly int EndIndex;

        public ReadOnlySpan<char> Text => LogicalLine.GetText(StartIndex, EndIndex - StartIndex);

        public LogicalLine LogicalLine;

        public PhysicalLine(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public override string ToString()
        {
            return Text.ToString();
        }
    }
}
