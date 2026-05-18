using System;
using System.Diagnostics;

namespace PESpy.ViewMap
{
    [DebuggerDisplay("{PhysicalStartPixel}-{PhysicalStartPixel+Width} ({Width})")]
    public struct VisualSection
    {
        public int PhysicalStartPixel;
        public int LogicalStartPixel;
        public int Width;
    }
}
