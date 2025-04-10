using System;
using System.Text;

namespace PESpy
{
    public readonly struct ImageSymbol : IValue
    {
        //Special section numbers
        public const short IMAGE_SYM_UNDEFINED = 0;
        public const short IMAGE_SYM_ABSOLUTE = -1;
        public const short IMAGE_SYM_SECTION_MAX = unchecked((short) 0xFEFF); //0xFF00-0xFFFF are special
        public const int IMAGE_SYM_SECTION_MAX_EX = int.MaxValue;

        public NameOrOffset Name { get; }

        public int Value { get; }

        public short SectionNumber { get; }

        public ImageSymType Type { get; }

        public ImageSymClass StorageClass { get; }

        public byte NumberOfAuxSymbols { get; }

        public ImageAuxSymbol[] AuxSymbols { get; }

        public int Offset { get; }

        internal const int StructSize =
            8 + //ShortName
            sizeof(int) + //Value
            sizeof(short) + //SectionNumber
            sizeof(short) + //Type
            sizeof(byte) + //StorageClass
            sizeof(byte); //NumberOfAuxSymbols

        internal ImageSymbol(IFileReader reader)
        {
            Offset = (int) reader.Position;

            //If the name is 8 bytes or less, it can be declared immediately inline. Otherwise,
            //the name is declared in the string table that immediately follows the list of symbols,
            //and the name contains a pointer into it. If the first 4 bytes of the name are all 0, then
            //the second 4 bytes is an offset into the string table. Otherwise, the name is the name
            Name = new NameOrOffset(reader);
            Value = reader.ReadInt32();
            SectionNumber = reader.ReadInt16();
            Type = (ImageSymType) reader.ReadInt16(); //I think you haev to use the N_ ype packing constants with this to extract the type + special derived types
            StorageClass = (ImageSymClass) reader.ReadByte();
            NumberOfAuxSymbols = reader.ReadByte();

            if (NumberOfAuxSymbols > 0)
            {
                var auxSymbols = new ImageAuxSymbol[NumberOfAuxSymbols];

                for (var i = 0; i < NumberOfAuxSymbols; i++)
                    auxSymbols[i] = new ImageAuxSymbol(reader);

                AuxSymbols = auxSymbols;
            }
            else
                AuxSymbols = Array.Empty<ImageAuxSymbol>();
        }

        public struct NameOrOffset
        {
            public string ShortName { get; }

            public int Short { get; }
            public int Long { get; }

            internal NameOrOffset(IFileReader reader)
            {
                var @short = reader.ReadInt32();
                var @long = reader.ReadInt32();

                if (@short == 0)
                {
                    Short = @short;
                    Long = @long;
                    ShortName = null;
                }
                else
                {
                    //Extract the bytes from the Int32's we read
                    var bytes = new byte[]
                    {
                        (byte) (@short & 0xFF),
                        (byte) ((@short >> 8) & 0xFF),
                        (byte) ((@short >> 16) & 0xFF),
                        (byte) ((@short >> 24) & 0xFF),
                        (byte) (@long & 0xFF),
                        (byte) ((@long >> 8) & 0xFF),
                        (byte) ((@long >> 16) & 0xFF),
                        (byte) ((@long >> 24) & 0xFF),
                    };

                    int nonPaddedLength = 0;

                    for (int i = bytes.Length; i > 0; --i)
                    {
                        if (bytes[i - 1] != 0)
                        {
                            nonPaddedLength = i;
                            break;
                        }
                    }

                    ShortName = Encoding.ASCII.GetString(bytes, 0, nonPaddedLength);
                    Short = 0;
                    Long = 0;
                }
            }

            public override string ToString()
            {
                if (ShortName != null)
                    return ShortName.ToString();

                return $"Long Name {Long}";
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
