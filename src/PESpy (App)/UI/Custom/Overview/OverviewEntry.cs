using PInvoke;

namespace PESpy.UI
{
    struct OverviewEntry
    {
        public OverviewCell Property;
        public OverviewCell Value;

        public string Source { get; }

        public RECT Rect { get; set; }

        internal OverviewEntry(string property, string? value, string source)
        {
            Property = new OverviewCell(property);
            Value = new OverviewCell(value);
            Source = source;
        }

        public override string ToString()
        {
            return Property.Value!.ToString();
        }
    }
}
