using PInvoke;

namespace PESpy.Tests
{
    internal class MockTextLine
    {
        public string Text { get; }

        public int Top { get; set; }

        public int Left { get; }

        public int Bottom => Top + Height;

        public int Right { get; }

        public int Width => Right - Left;

        public int Height { get; }

        internal MockTextLine(string text, int left, int top, int right, int height)
        {
            Text = text;
            Left = left;
            Top = top;
            Right = right;
            Height = height;
        }

        internal bool Intersects(in RECT rect)
        {
            return Left < rect.right &&
                Right > rect.left &&
                Top < rect.bottom &&
                Bottom > rect.top;
        }

        public override string ToString()
        {
            return $"[{Top}-{Bottom}] {Text}";
        }
    }
}
