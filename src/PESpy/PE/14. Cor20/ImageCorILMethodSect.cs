using System;
using ClrDebug;
using PESpy.View;

namespace PESpy
{
    //Describes a section header. Contained in another structure such as ImageCorILMethodSectEH based on the value found in Kind
    public readonly struct ImageCorILMethodSect : IValue, IViewable //IMAGE_COR_ILMETHOD_SECT_SMALL / IMAGE_COR_ILMETHOD_SECT_FAT
    {
        public CorILMethodSect Kind { get; }

        public int DataSize { get; }

        public int Offset { get; }

        internal int StructSize => (Kind & CorILMethodSect.FatFormat) != 0 ? sizeof(int) : sizeof(short);

        internal ImageCorILMethodSect(CorILMethodSect kind, in MemoryChunk chunk, out int read)
        {
            Offset = chunk.AbsoluteOffset;

            Kind = kind;

            if ((kind & CorILMethodSect.FatFormat) != 0)
            {
                //The data pointed to by the section is in fat format, and its length is encoded in 3 bytes
                DataSize = chunk.PeekInt32(0) >> 8;
                read = 4;
            }
            else
            {
                //The data pointed to by the section is in small format, and its length is a single byte
                DataSize = chunk.PeekByte(1);
                read = 2;
            }
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            var isFat = (Kind & CorILMethodSect.FatFormat) != 0;

            return writer.NewStruct(
                isFat ? Strings.IMAGE_COR_ILMETHOD_SECT_FAT : Strings.IMAGE_COR_ILMETHOD_SECT_SMALL,
                this,
                ViewKind.ImageCorILMethodSect,
                StructSize
            );
        }

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            var isFat = (Kind & CorILMethodSect.FatFormat) != 0;

            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Kind), Kind, sizeof(byte));

            if (isFat)
            {
                using (var b = s.WriteBitFields<int>())
                {
                    b.WriteField(nameof(Kind), Kind, 8);
                    b.WriteField(nameof(DataSize), DataSize, 24);
                }
            }
            else
                s.WriteField(nameof(DataSize), (byte) DataSize);

            return s.ToArray();
        }
    }
}
