#if !DISABLE_PINVOKE
namespace PESpy.Overview
{
    struct OverviewRow
    {
        public OverviewEntry Left { get; set; }

        public OverviewEntry? Right { get; set; }
    }
}
#endif
