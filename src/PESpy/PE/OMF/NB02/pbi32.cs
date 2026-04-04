using System;
using PESpy.PDB;
using PESpy.View;

namespace PESpy
{
    //Name is guessed based on the fact the 16-bit version is called pbi, and in cvexefmt.h
    //nsg and smd just had "32" added to the end
    public readonly struct pbi32 : IValue, IViewable
    {
        private const int offOffset = 0;
        private const int segOffset = 4;
        private const int typeOffset = 6;
        private const int nameOffset = 8;

        public int off => chunk.PeekInt32(offOffset);

        public ISECT seg => chunk.PeekUInt16(segOffset);

        public ushort type => chunk.PeekUInt16(typeOffset);

        public SymString name => chunk.PeekSymString(nameOffset, isLengthPrefixed: true);

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(int) + //off
            sizeof(short) + //seg
            sizeof(short); //type

        internal int StructSize => FixedStructSize + name.Length + 1;

        private readonly MemoryChunk chunk;

        internal pbi32(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.pbi32, this, ViewKind.pbi32, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 1:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 2:
                    structWriter.WriteField(nameof(type), typeOffset, type);
                    break;

                case 3:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, name);
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
