using PInvoke;

namespace PESpy
{
    public struct CreateParams
    {
        public const int CW_USEDEFAULT = -2147483648;

        public static readonly CreateParams Default = new CreateParams
        {
            X = CW_USEDEFAULT,
            Y = CW_USEDEFAULT,
            Width = CW_USEDEFAULT,
            Height = CW_USEDEFAULT
        };

        public string? ClassName { get; set; }

        public string? Caption { get; set; }

        public int Style { get; set; }

        public WINDOW_EX_STYLE ExStyle { get; set; }

        public WNDCLASS_STYLES ClassStyle { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public HWND Parent { get; set; }
    }
}
