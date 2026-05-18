using System.Diagnostics;

namespace PESpy.Overview
{
    public struct Bound
    {
        public int Left;
        public int Width;

        public int Right => Left + Width;

        internal Bound(int left, int width)
        {
            Debug.Assert(width >= 0);
            Left = left;
            Width = width;
        }
    }
}
