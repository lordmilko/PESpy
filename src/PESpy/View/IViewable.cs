#if !DEBUG_POSITION
#endif

namespace PESpy.View
{
    /// <summary>
    /// Represents an entity that is capable of being transformed into a view.
    /// </summary>
    public interface IViewable
    {
        void WriteGlobals(ViewWriter writer);

        IView? WriteStruct(ViewWriter writer);

        //We need to pass our parent view, because the offset of the IValue may have been changed to be in a different mode (e.g. physical or virtual)
        //when the parent struct was created
        IView[] GetChildren(IView parent, ViewWriter writer);
    }
}
