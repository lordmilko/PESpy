namespace PESpy.View
{
    public interface IViewWriter
    {
        void WriteField<T>(string name, T value);
    }

    /// <summary>
    /// Represents an entity that is capable of being transformed into a view.
    /// </summary>
    public interface IViewable
    {
        void WriteGlobals(ViewWriter writer);

        IView? WriteStruct(ViewWriter writer);

        //This is a method instead of a property so we don't see any noise in the debugger display; particularly for IFile implementations, which are also IViewable
        int NumChildren();

        void WriteChild(int index, ref StructWriter structWriter);
    }

    internal interface IViewableValue : IViewable, IValue
    {
    }
}
