using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct UnwindMapEntry : IValue, IViewable
    {
        private const int ToStateOffset = 0;
        private const int ActionOffset = 4;

        public int ToState => chunk.PeekInt32(ToStateOffset);

        public int Action => chunk.PeekInt32(ActionOffset);

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

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("toState", ToStateOffset, ToState);
                    break;

                case 1:
                    structWriter.WriteField("action", ActionOffset, Action);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
