using System;
using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("{PhysicalStartPixel}-{PhysicalStartPixel+Width} ({Width})")]
    public struct VisualSection
    {
        public int PhysicalStartPixel;
        public int LogicalStartPixel;
        public int Width;
        public (long startAddress, long pixelAddress, IntPtr pViewByte)[] Data; //startAddress is the address that the entity starts. pixelAddress is the address of the pixel (which might be part of the body)
        public (int startPixel, int endPixel, int directoryIndex)[]? Directories;

        public int GetBestPixel(long address, int lo, int hi)
        {
            var data = Data;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                ref var item = ref data[mid];

                if (item.pixelAddress > address)
                    hi = mid - 1;
                else if (item.pixelAddress < address)
                    lo = mid + 1;
                else
                    return mid;
            }

            return lo;
        }
    }
}
