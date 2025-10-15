using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        public readonly struct Location : IValue, IViewable
        {
            private const int OffsetOffset = 0;
            private const int SizeOffset = 8;

            public long Offset => chunk.PeekInt64(OffsetOffset);
            public long Size => chunk.PeekInt64(SizeOffset);

            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(long) + //Offset
                sizeof(long);  //Size

            private readonly MemoryChunk chunk;

            internal Location(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.location_t, this, ViewKind.BundleLocation, StructSize);

        int IViewable.NumChildren => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("offset", OffsetOffset, Offset);
                    break;

                case 1:
                    structWriter.WriteField("size", SizeOffset, Size);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
        }
    }
}
