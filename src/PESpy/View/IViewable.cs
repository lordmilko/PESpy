#if !DEBUG_POSITION
#endif

namespace PESpy.View
{
    /// <summary>
    /// Represents an entity that is capable of being transformed into a view.
    /// </summary>
    public interface IViewable
    {
        void WriteView(ViewWriter writer);
    }
}
