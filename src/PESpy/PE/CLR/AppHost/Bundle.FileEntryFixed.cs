using System;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        private const int OffsetOffset = 8;
        private const int SizeOffset = 8;
        private const int CompressedSizeOffset = 16;
        private const int TypeOffsetWithCompressedSize = 24;
        private const int TypeOffsetWithoutCompressedSize = 16;

        //file_entry_fixed_t
        public readonly struct FileEntryFixed : IValue, IViewable
        {
            public long Offset => chunk.PeekInt64(OffsetOffset);

            public long Size => chunk.PeekInt64(SizeOffset);

            public long CompressedSize => hasCompressedSize ? chunk.PeekInt64(CompressedSizeOffset) : 0;

            public file_type_t Type => (file_type_t) chunk.PeekByte(hasCompressedSize ? TypeOffsetWithCompressedSize : TypeOffsetWithoutCompressedSize);

            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(long) + //Offset
                sizeof(long) + //Size
                               //CompressedSize is optional
                sizeof(byte);  //Type

            public int StructSize => FixedStructSize + (hasCompressedSize ? 8 : 0);

            private readonly MemoryChunk chunk;
            private readonly bool hasCompressedSize;

            internal FileEntryFixed(in MemoryChunk chunk, bool hasCompressedSize)
            {
                this.chunk = chunk;
                this.hasCompressedSize = hasCompressedSize;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.file_entry_fixed_t, this, ViewKind.BundleFileEntryFixed, StructSize);

            int IViewable.NumChildren => hasCompressedSize ? 4 : 3;

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

                    case 2:
                        if (hasCompressedSize)
                            structWriter.WriteField("compressedSize", CompressedSizeOffset, CompressedSize);
                        else
                            structWriter.WriteField("type", TypeOffsetWithoutCompressedSize, Type, sizeof(byte));

                        break;

                    case 3:
                        if (hasCompressedSize)
                            structWriter.WriteField("type", TypeOffsetWithCompressedSize, Type, sizeof(byte));
                        else
                            throw new IndexOutOfRangeException();

                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
