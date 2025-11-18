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

        public readonly int StartTextIndex;
        public readonly int EndTextIndex;

        public readonly int StartFormatIndex;
        public readonly int EndFormatIndex;

        public ReadOnlySpan<char> Text => LogicalLine.GetText(StartTextIndex, EndTextIndex - StartTextIndex);

        public ReadOnlySpan<ViewByteFormatRange> FormatRanges => LogicalLine.GetRanges(StartFormatIndex, EndFormatIndex - StartFormatIndex);

        public LogicalLine LogicalLine;

        public PhysicalLine(
            int startTextIndex,
            int endTextIndex,
            int startFormatIndex,
            int endFormatIndex)
        {
            StartTextIndex = startTextIndex;
            EndTextIndex = endTextIndex;

            StartFormatIndex = startFormatIndex;
            EndFormatIndex = endFormatIndex;
        }

        public override string ToString()
        {
            return Text.ToString();
        }
    }
}
