using PESpy.View;

namespace PESpy
{
    public readonly struct UnwindMapEntry : IValue, IViewable
    {
        public int ToState { get; }

        public int Action { get; }

        public int Offset { get; }

        internal UnwindMapEntry(ref FileReader reader)
        {
            Offset = (int) reader.Position;

            ToState = reader.ReadInt32();
            Action = reader.ReadInt32();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.UnwindMapEntry), this, ViewKind.UnwindMapEntry);

            s.WriteField("toState", ToState);
            s.WriteField("action", Action);
        }
    }
}
