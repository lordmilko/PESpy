using System;
using System.Text;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public struct ImageSymbol : IValue, IViewable
    {
        //Special section numbers
        public const short IMAGE_SYM_UNDEFINED = 0;
        public const short IMAGE_SYM_ABSOLUTE = -1;
        public const short IMAGE_SYM_SECTION_MAX = unchecked((short) 0xFEFF); //0xFF00-0xFFFF are special
        public const int IMAGE_SYM_SECTION_MAX_EX = int.MaxValue;

        public NameOrOffset Name { get; }

#if PEFAST
        public uint Value => chunk.PeekUInt32(8);
#else
        public uint Value { get; }
#endif

#if PEFAST
        public short SectionNumber => chunk.PeekInt16(12);
#else
        public short SectionNumber { get; }
#endif

#if PEFAST
        public ImageSymType Type => (ImageSymType) chunk.PeekUInt16(14);
#else
        public ImageSymType Type { get; }
#endif

#if PEFAST
        public ImageSymClass StorageClass => (ImageSymClass) chunk.PeekByte(16);
#else
        public ImageSymClass StorageClass { get; }
#endif

#if PEFAST
        public byte NumberOfAuxSymbols => chunk.PeekByte(17);
#else
        public byte NumberOfAuxSymbols { get; }
#endif

public ImageAuxSymbol[] AuxSymbols { get; }

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

        internal const int StructSize =
            8 + //Name (both halves)
            sizeof(int) + //Value
            sizeof(short) + //SectionNumber
            sizeof(short) + //Type
            sizeof(byte) + //StorageClass
            sizeof(byte); //NumberOfAuxSymbols

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageSymbol(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            AuxSymbols = default!;

            /* If the name is 8 bytes or less, it can be declared immediately inline. Otherwise,
             * the name is declared in the string table that immediately follows the list of symbols,
             * and the name contains a pointer into it. If the first 4 bytes of the name are all 0, then
             * the second 4 bytes is an offset into the string table. Otherwise, the name is the name */
            Name = new NameOrOffset(chunk);

            var numAux = NumberOfAuxSymbols;

            if (numAux == 0)
                AuxSymbols = Array.Empty<ImageAuxSymbol>();
            else
            {
                var results = new ImageAuxSymbol[numAux];

                var read = 18;

                for (var i = 0; i < numAux; i++)
                {
                    var aux = new ImageAuxSymbol(chunk.Slice(read));

                    read += ImageAuxSymbol.StructSize;

                    results[i] = aux;
                }

                AuxSymbols = results;
            }
        }
#else
        internal ImageSymbol(IFileReader reader)
        {
            Offset = (int) reader.Position;

            //If the name is 8 bytes or less, it can be declared immediately inline. Otherwise,
            //the name is declared in the string table that immediately follows the list of symbols,
            //and the name contains a pointer into it. If the first 4 bytes of the name are all 0, then
            //the second 4 bytes is an offset into the string table. Otherwise, the name is the name
            Name = new NameOrOffset(reader);
            Value = reader.ReadUInt32();
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
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_SYMBOL), this, ViewKind.ImageSymbol);

            if (Name.ShortName == null)
            {
                s.WriteField("Name.Short", Name.Short);
                s.WriteField("Name.Long", Name.Long);
            }
            else
            {
                s.WriteNullPaddedUTF8Field(nameof(Name), Name.ShortName, 8);
            }

            s.WriteField(nameof(Value), Value);
            s.WriteField(nameof(SectionNumber), SectionNumber);
            s.WriteField(nameof(Type), Type, sizeof(short));
            s.WriteField(nameof(StorageClass), StorageClass, sizeof(byte));
            s.WriteField(nameof(NumberOfAuxSymbols), NumberOfAuxSymbols);
            s.WriteInline(AuxSymbols);
        }

        public struct NameOrOffset
        {
            public string? ShortName { get; }

            public int Short { get; }
            public int Long { get; }

#if PEFAST
            internal NameOrOffset(in MemoryChunk chunk)
            {
                var @short = chunk.PeekInt32(0);
                var @long = chunk.PeekInt32(4);
#else
            internal NameOrOffset(IFileReader reader)
            {
                var @short = reader.ReadInt32();
                var @long = reader.ReadInt32();
#endif

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
