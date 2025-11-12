using System;
using System.Diagnostics;

namespace PESpy
{
    
    [DebuggerDisplay("Count = {Count}")]
    public struct LogicalLineCollection
    {
        private GapBuffer<LogicalLine> _lines;

        public int Count => _lines.Count;

        internal PhysicalLine FirstVisibleLine => _lines[0].FirstVisibleLine;

        internal PhysicalLine FirstPhysicalLine => _lines[0].Lines[0];

        internal LogicalLine FirstLogicalLine => _lines[0];

        internal PhysicalLine LastVisibleLine => _lines[_lines.Count - 1].LastVisibleLine;

        internal PhysicalLine LastPhysicalLine
        {
            get
            {
                var logicalLine = _lines[_lines.Count - 1];
                return logicalLine.Lines[logicalLine.Lines.Length - 1];
            }
        }

        internal LogicalLine LastLogicalLine => _lines[_lines.Count - 1];

        internal LogicalLineCollection(int initialCapacity)
        {
            _lines = new GapBuffer<LogicalLine>(initialCapacity);
        }

        public LogicalLine this[int index] => _lines[index];

        public void Clear() => _lines.Clear();

        public void Append(Span<LogicalLine> logicalLines)
        {
            _lines.Append(logicalLines);
        }

        public void Prepend(Span<LogicalLine> logicalLines)
        {
            _lines.Prepend(logicalLines);
        }

        public void RemoveLeadingLines(int numLinesToRemove)
        {
            //We're being asked to remove _physical_ lines. Iterate from the top and identify whether a given
            //logical line should be removed, or whether some of its lines should just be marked as hidden

            var lastLogicalLineToRemove = -1;

            for (var i = 0; i < _lines.Count; i++)
            {
                var logicalLine = _lines[i];

                var numVisibleLines = logicalLine.NumVisibleLines;

                if (numLinesToRemove >= numVisibleLines)
                {
                    //All of the (potentially remaining) lines in this logical line should be removed
                    lastLogicalLineToRemove = i;
                    numLinesToRemove -= numVisibleLines;
                }
                else
                {
                    //Mark the first n lines as hidden
                    for (var j = 0; j < numLinesToRemove; j++)
                        logicalLine.Lines[j].IsVisible = false;

                    numLinesToRemove = 0;
                }

                if (numLinesToRemove == 0)
                    break;
            }

            if (lastLogicalLineToRemove == -1)
                return; //Done

            //Remove all lines up to lastLogicalLineToRemove (+1 because thats the count. e.g. remove line 0? from index 0 remove 1)
            _lines.RemoveRange(0, lastLogicalLineToRemove + 1);
        }

        public void RemoveTrailingLines(int numLinesToRemove)
        {
            //Basically, the same as the above but in reverse

            var lastLogicalLineToRemove = -1;

            for (var i = _lines.Count - 1; i >= 0; i--)
            {
                var logicalLine = _lines[i];

                var numVisibleLines = logicalLine.NumVisibleLines;

                if (numLinesToRemove >= numVisibleLines)
                {
                    //All of the (potentially remaining) lines in this logical line should be removed
                    lastLogicalLineToRemove = i;
                    numLinesToRemove -= numVisibleLines;
                }
                else
                {
                    //Mark the last n lines as hidden
                    for (var j = numLinesToRemove; j > 0; j--)
                        logicalLine.Lines[j].IsVisible = false;

                    numLinesToRemove = 0;
                }

                if (numLinesToRemove == 0)
                    break;
            }

            if (lastLogicalLineToRemove == -1)
                return; //Done

            //Remove these lines from the _end_
            _lines.RemoveRange(lastLogicalLineToRemove, _lines.Count - lastLogicalLineToRemove);
        }
    }
}
