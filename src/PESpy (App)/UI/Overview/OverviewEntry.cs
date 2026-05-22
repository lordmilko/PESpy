#if !DISABLE_PINVOKE
using PInvoke;

namespace PESpy.Overview
{
    struct OverviewEntry
    {
        public OverviewCell Property;
        public OverviewCell Value;

        public string? Source { get; }

        public RECT Rect { get; set; }

        //Certain values we want to show in black even if their value is false, because they inherently
        //apply to the file
        public bool ForceEnabled { get; set; }

        internal OverviewEntry(string property, string? value, string? source, bool forceEnabled = false)
        {
            Property = new OverviewCell(property);
            Value = new OverviewCell(value);
            Source = source;
            ForceEnabled = forceEnabled;
        }

        public override string ToString()
        {
            return Property.Value!.ToString();
        }
    }
}
#endif
