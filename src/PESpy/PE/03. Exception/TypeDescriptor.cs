using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct TypeDescriptor : IValue, IViewable
    {
        private const int pVFTableOffset = 0;
        private const int SpareOffset = 8;
        private const int NameOffset = 16;

        public ulong pVFTable => chunk.PeekUInt64(pVFTableOffset);

        public ulong Spare => chunk.PeekUInt64(SpareOffset);

        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(NameOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(long) + //pVFTable
            sizeof(long) + //Spare
            Name.Length + 1; //Name

        private readonly MemoryChunk chunk;

        internal TypeDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TypeDescriptor, this, ViewKind.TypeDescriptor, StructSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WritePointerField("pVFTable", pVFTableOffset, pVFTable);
                    break;

                case 1:
                    structWriter.WritePointerField("spare", SpareOffset, Spare);
                    break;

                case 2:
                    structWriter.WriteAnsiNullTerminatedField("name", NameOffset, Name);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
