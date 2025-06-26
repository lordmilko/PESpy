using PESpy.View;

namespace PESpy
{
    public readonly struct UnwindMapEntry : IValue, IViewable
    {
#if PEFAST
        public int ToState => chunk.PeekInt32(0);
#else
        public int ToState { get; }
#endif

#if PEFAST
        public int Action => chunk.PeekInt32(4);
#else
        public int Action { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //ToState
            sizeof(int);  //Action

#if PEFAST
        private readonly MemoryChunk chunk;

        internal UnwindMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

#else
        internal UnwindMapEntry(IFileReader reader)
        {
            Offset = (int) reader.Position;

            ToState = reader.ReadInt32();
            Action = reader.ReadInt32();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(PESpy.Native.UnwindMapEntry), this, ViewKind.UnwindMapEntry, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("toState", ToState);
            s.WriteField("action", Action);

            return s.ToArray();
        }
    }
}
