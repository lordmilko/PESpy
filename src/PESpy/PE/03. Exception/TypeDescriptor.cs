using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct TypeDescriptor : IValue, IViewable
    {
        private const int pVFTableOffset = 0;
        private const int spareOffset = 8;
        private const int nameOffset = 16;

        public ulong pVFTable { get; }

        public ulong spare { get; }

        public AnsiString name { get; }

        public int Offset { get; }

        internal int StructSize =>
            sizeof(long) + //pVFTable
            sizeof(long) + //spare
            name.Length + 1; //name


        internal TypeDescriptor(in MemoryChunk chunk)
        {
            //HandlerType4 needs to take the address of itself to expose its
            //continuationAddresses which it can't do if TypeDescriptor stores a MemoryChunk; so we need
            //to eagerly read here
            Offset = chunk.AbsoluteOffset;

            pVFTable = chunk.PeekUInt64(pVFTableOffset);
            spare = chunk.PeekUInt64(spareOffset);
            name = chunk.PeekAnsiNullTerminatedString(nameOffset);
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.TypeDescriptor, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WritePointerField(nameof(pVFTable), pVFTableOffset, pVFTable);
                    break;

                case 1:
                    structWriter.WritePointerField(nameof(spare), spareOffset, spare);
                    break;

                case 2:
                    structWriter.WriteAnsiNullTerminatedField(nameof(name), nameOffset, name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
