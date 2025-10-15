using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBDDDynamicRelocation : IValue, IViewable
    {
        private const int LeftOffset = 0;
        private const int RightOffset = 2;
        private const int ValueOffset = 4;

        /// <summary>
        /// Index of FALSE edge in BDD array
        /// </summary>
        public short Left => chunk.PeekInt16(LeftOffset);

        /// <summary>
        /// Index of TRUE edge in BDD array
        /// </summary>
        public short Right => chunk.PeekInt16(RightOffset);

        /// <summary>
        /// Either FeatureNumber or Index into RVAs array
        /// </summary>
        public int Value => chunk.PeekInt32(ValueOffset);

        public const int StructSize =
            sizeof(short) + //Left
            sizeof(short) + //Right
            sizeof(int);    //Value

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageBDDDynamicRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BDD_DYNAMIC_RELOCATION, this, ViewKind.ImageBDDDynamicRelocation, StructSize);

        int IViewable.NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Left), LeftOffset, Left);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Right), RightOffset, Right);
                    break;

                case 2:
                    structWriter.WriteField(nameof(Value), ValueOffset, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
