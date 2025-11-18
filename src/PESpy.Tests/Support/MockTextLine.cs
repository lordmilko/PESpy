using System.Diagnostics;
using PInvoke;

namespace PESpy.Tests
{
    internal class MockTextLine
    {
        public string Text { get; private set; }

        public int Top { get; set; }

        public int Left { get; }

        public int Bottom => Top + Height;

        public int Right { get; set; }

        public int Width => Right - Left;

        public int Height { get; set; }

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

        internal bool IsEnclosedBy(in RECT rect)
        {
            return rect.left <= Left &&
               rect.top <= Top &&
               rect.right >= Right &&
               rect.bottom >= Bottom;
        }

        internal void MergeWith(MockTextLine other)
        {
            Debug.Assert(Top == other.Top);
            Debug.Assert(Bottom == other.Bottom);
            Text += other.Text;
            Right = other.Right;
        }

        public override string ToString()
        {
            return $"[{Top}-{Bottom}] {Text}";
        }
    }
}
