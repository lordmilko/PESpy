using PInvoke;

namespace PESpy.UI
{
    public struct OverviewCell
    {
        //Actual visible width is stored in the _columnWidths on the panel
        public int ContentWidth;
        public int Left;
        public int Top;
        public string? Value;

        public OverviewCell(string? value)
        {
            Value = value;
        }

        public unsafe void MeasureContent(HDC hdc)
        {
            const int padding = 15;

            if (Value == null)
                ContentWidth = padding;
            else
            {
                Gdi32.GetTextExtentPoint32W(hdc, Value, out var size);
                ContentWidth = size.cx + padding;
            }
        }
    }
}
