using ClrDebug;
using PESpy.View;

namespace PESpy
{
    //Describes a section header. Contained in another structure such as ImageCorILMethodSectEH based on the value found in Kind
    public readonly struct ImageCorILMethodSect : IViewableValue //IMAGE_COR_ILMETHOD_SECT_SMALL / IMAGE_COR_ILMETHOD_SECT_FAT
    {
        public CorILMethodSect Kind { get; }

        public int DataSize { get; }

        public int Offset { get; }

        internal int StructSize => (Kind & CorILMethodSect.FatFormat) != 0 ? FatSize : TinySize;

        internal const int FatSize = sizeof(int);
        internal const int TinySize = sizeof(short);

        internal ImageCorILMethodSect(in MemoryChunk chunk, bool isFat) : this(isFat ? CorILMethodSect.FatFormat : default, chunk, out _)
        {
        }

        internal ImageCorILMethodSect(CorILMethodSect kind, in MemoryChunk chunk, out int read)
        {
            Offset = chunk.AbsoluteOffset;

            Kind = kind;

            if ((kind & CorILMethodSect.FatFormat) != 0)
            {
                //The data pointed to by the section is in fat format, and its length is encoded in 3 bytes
                DataSize = chunk.PeekInt32(0) >> 8;
                read = 4; //Kind (1) + DataSize (3)
            }
            else
            {
                //The data pointed to by the section is in small format, and its length is a single byte
                DataSize = chunk.PeekByte(1);
                read = 2; //Kind (1) + DataSize (1)
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            var isFat = (Kind & CorILMethodSect.FatFormat) != 0;

            if (isFat)
            {
                return writer.NewStruct(
                    Strings.IMAGE_COR_ILMETHOD_SECT_FAT,
                    this,
                    ViewKind.ImageCorILMethodSectFat,
                    StructSize
                );
            }
            else
            {
                return writer.NewStruct(
                    Strings.IMAGE_COR_ILMETHOD_SECT_SMALL,
                    this,
                    ViewKind.ImageCorILMethodSectSmall,
                    StructSize
                );
            }
        }

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            var isFat = (Kind & CorILMethodSect.FatFormat) != 0;

            switch (index)
            {
                case 0:
                    if (isFat)
                        structWriter.WriteBitField(nameof(Kind), relativeOffset: 0, Kind, sizeof(int), 8);
                    else
                        structWriter.WriteField(nameof(Kind), relativeOffset: 0, Kind, sizeof(byte));

                    break;

                case 1:
                    if (isFat)
                        structWriter.WriteBitField(nameof(DataSize), relativeOffset: 0, DataSize, sizeof(int), 24);
                    else
                        structWriter.WriteField(nameof(DataSize), relativeOffset: 1, (byte) DataSize);

                    break;
            }
        }
    }
}
