using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a Profile Guided Optimization entry. This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct PogoItem : IViewableValue
    {
        private const int RVAOffset = 0;
        private const int SizeOffset = 4;
        private const int NameOffset = 8;

        public int RVA => chunk.PeekInt32(RVAOffset);

        public int Size => chunk.PeekInt32(SizeOffset);

        public AnsiString Name => chunk.PeekAnsiNullTerminatedString(NameOffset);

        internal const int FixedStructSize =
            sizeof(int) + //RVA
            sizeof(int); //Size

        internal int StructSize =>
            FixedStructSize +
            Name.Length + 1; //Name

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal PogoItem(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.PogoItem, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(RVA), RVAOffset, RVA);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    break;

                case 2:
                    structWriter.WriteAnsiNullTerminatedField(nameof(Name), NameOffset, Name);
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
