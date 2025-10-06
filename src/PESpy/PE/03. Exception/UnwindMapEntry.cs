using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct UnwindMapEntry : IValue, IViewable
    {
        public int ToState => chunk.PeekInt32(0);

        public int Action => chunk.PeekInt32(4);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ToState
            sizeof(int);  //Action

        private readonly MemoryChunk chunk;

        internal UnwindMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.UnwindMapEntry, this, ViewKind.UnwindMapEntry, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("toState", ToState);
            s.WriteField("action", Action);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
