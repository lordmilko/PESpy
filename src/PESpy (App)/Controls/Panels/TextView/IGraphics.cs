using System;
using PInvoke;

namespace PESpy
{
    public interface IGraphics : IDisposable
    {
        /// <summary>
        /// Gets the height of a single line of text.
        /// </summary>
        int LineHeight { get; }

        public HDC MemDC { get; }

        void Initialize();

        void CreateMemoryBitmap(int width, int height);

        void FillBackground(in RECT rect);

        void DrawText(ReadOnlySpan<char> str, int left, int top, ref int right, int fontHeight);

        //Shift memory bits around (used for scrolling)
        void ScrollMemDC(int destX, int destY, int width, int height, int sourceX, int sourceY);

        int GetVerticalScrollThumb();

        void SetScrollInfo(SCROLLBAR_CONSTANTS scrollBar, in SCROLLINFO scrollInfo);

        //Commit changes in the memory DC to the screen, or a destination DC if one is provided
        void CommitMemDC(int destX, int destY, int width, int height, int sourceX, int sourceY, HDC destinationDC = default);
    }
}
