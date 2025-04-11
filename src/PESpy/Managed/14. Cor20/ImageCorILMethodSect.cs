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

        internal ImageCorILMethodSect(CorILMethodSect kind, IFileReader reader)
        {
            //Already read kind byte
            Offset = (int) reader.Position - 1;

            Kind = kind;

            if (kind.HasFlag(CorILMethodSect.FatFormat))
            {
            }
            else
            {
                //The data pointed to by the section is in small format, and its length is a single byte
                DataSize = reader.ReadByte();
            }
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            var isFat = Kind.HasFlag(CorILMethodSect.FatFormat);

            using var s = writer.CreateStruct(
                isFat ? nameof(IMAGE_COR_ILMETHOD_SECT_FAT) : nameof(IMAGE_COR_ILMETHOD_SECT_SMALL),
                this,
                ViewKind.ImageCorILMethodSect
            );

            s.WriteField(nameof(Kind), Kind, sizeof(byte));

            if (isFat)
                throw new System.NotImplementedException();
            else
                s.WriteField(nameof(DataSize), (byte) DataSize);
        }
    }
}
