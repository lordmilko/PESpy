using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct IptoStateMapEntry : IValue, IViewable
    {
        private const int IpOffset = 0;
        private const int StateOffset = 4;
        public int Ip => chunk.PeekInt32(IpOffset);

        public int State => chunk.PeekInt32(StateOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //Ip
            sizeof(int);  //State

        private readonly MemoryChunk chunk;

        internal IptoStateMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IptoStateMapEntry, this, ViewKind.IptoStateMapEntry, StructSize);

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Ip), IpOffset, Ip);
                    break;

                case 1:
                    structWriter.WriteField(nameof(State), StateOffset, State);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
