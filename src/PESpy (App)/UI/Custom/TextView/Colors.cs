using PInvoke;

namespace PESpy.UI
{
    internal static class Colors
    {
        public static readonly COLORREF Address = new COLORREF(0, 0, 0); //Black
        public static readonly COLORREF Code = new COLORREF(0, 0, 128); //Navy blue
        public static readonly COLORREF Number = new COLORREF(0, 128, 0); //Green
        public static readonly COLORREF Symbol = new COLORREF(0, 0, 255); //Bright blue
        public static readonly COLORREF Import = new COLORREF(255, 0, 255); //Magenta
        public static readonly COLORREF Byte = new COLORREF(0, 128, 64); //Earth green
        public static COLORREF String => Number;
        public static readonly COLORREF Line = new COLORREF(128, 128, 128); //Gray
    }
}
