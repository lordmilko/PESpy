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

        int NumChildren { get; }

        void WriteChild(int index, ref StructWriter structWriter);
    }

    internal interface IViewableValue : IViewable, IValue
    {
    }
}
