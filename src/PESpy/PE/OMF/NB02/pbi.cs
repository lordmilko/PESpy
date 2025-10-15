using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //SSTPUBLICS
    public readonly struct pbi : IValue, IViewable
    {
        private const int offOffset = 0;
        private const int segOffset = 2;
        private const int typeOffset = 4;
        private const int nameOffset = 6;

        public ushort off => chunk.PeekUInt16(offOffset);

        public ushort seg => chunk.PeekUInt16(segOffset);

        public ushort type => chunk.PeekUInt16(typeOffset);

        public FixedAnsiString name
        {
            get
            {
                var length = chunk.PeekByte(nameOffset);
                return chunk.PeekAnsiFixedLength(7, length);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(short) + //off
            sizeof(short) + //seg
            sizeof(short); //type

        internal int StructSize => FixedStructSize + name.Length + 1;

        private readonly MemoryChunk chunk;

        internal pbi(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.pbi, this, ViewKind.pbi, StructSize);

        int IViewable.NumChildren => 4;

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
                    structWriter.WriteLengthPrefixedAnsiField(nameof(name), nameOffset, name);
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
