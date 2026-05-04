using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct UnwindMapEntry : IValue, IViewable
    {
        private const int toStateOffset = 0;
        private const int actionOffset = 4;

        public int toState => chunk.PeekInt32(toStateOffset);

        public int action => chunk.PeekInt32(actionOffset);

        public long Offset => chunk.AbsoluteOffset;

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
            writer.WriteUniqueRVAXRef(Offset, actionOffset, action);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.UnwindMapEntry, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(toState), toStateOffset, toState);
                    break;

                case 1:
                    structWriter.WriteField(nameof(action), actionOffset, action);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
