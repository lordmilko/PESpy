using System;
using System.Diagnostics;
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

        public const ushort N_BTMASK = 0x000F;
        public const ushort N_TMASK = 0x0030;
        //N_TMASK1 and N_TMASK2 don't seem to be used
        public const ushort N_BTSHIFT = 4;
        public const ushort N_TSHIFT = 2;

        public NameOrOffset Name { get; }

        public uint Value => chunk.PeekUInt32(8);

        public ushort SectionNumber => chunk.PeekUInt16(12);

        public ImageSymType Type => (ImageSymType) chunk.PeekUInt16(14);

        public ImageSymType BasicType => (ImageSymType) ((ushort) Type & N_BTMASK);

        public ImageSymDType DerivedType => (ImageSymDType) (((ushort) Type & N_TMASK) >> N_BTSHIFT);

        public ImageSymClass StorageClass => (ImageSymClass) chunk.PeekByte(16);

        public byte NumberOfAuxSymbols => chunk.PeekByte(17);

        public ImageAuxSymbol[] AuxSymbols { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            8 + //Name (both halves)
            sizeof(int) + //Value
            sizeof(short) + //SectionNumber
            sizeof(short) + //Type
            sizeof(byte) + //StorageClass
            sizeof(byte); //NumberOfAuxSymbols

        private readonly MemoryChunk chunk;

        internal ImageSymbol(in MemoryChunk chunk, CoffSymbolTable symbolTable)
        {
            this.chunk = chunk;
            AuxSymbols = default!;

            /* If the name is 8 bytes or less, it can be declared immediately inline. Otherwise,
             * the name is declared in the string table that immediately follows the list of symbols,
             * and the name contains a pointer into it. If the first 4 bytes of the name are all 0, then
             * the second 4 bytes is an offset into the string table. Otherwise, the name is the name */
            Name = new NameOrOffset(chunk, symbolTable);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_SYMBOL, this, ViewKind.ImageSymbol, StructSize + (NumberOfAuxSymbols * ImageAuxSymbol.StructSize));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            if (Name.Short == 0)
            {
                s.WriteField("Name.Short", Name.Short);
                s.WriteField("Name.Long", Name.Long);
            }
            else
            {
                s.WriteNullPaddedAnsiField(nameof(Name), Name.Name, 8);
            }

            s.WriteField(nameof(Value), Value);
            s.WriteField(nameof(SectionNumber), SectionNumber);
            s.WriteField(nameof(Type), Type, sizeof(short));
            s.WriteField(nameof(StorageClass), StorageClass, sizeof(byte));
            s.WriteField(nameof(NumberOfAuxSymbols), NumberOfAuxSymbols);
            s.WriteInline(AuxSymbols);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public readonly struct NameOrOffset
        {
            public FixedAnsiString Name { get; }

            public int Short { get; }
            public int Long { get; }

            internal NameOrOffset(in MemoryChunk chunk, CoffSymbolTable symbolTable)
            {
                var @short = chunk.PeekInt32(0);
                var @long = chunk.PeekInt32(4);

                if (@short == 0)
                {
                    Short = @short;
                    Long = @long;
                    Name = (FixedAnsiString) symbolTable.GetString(Long);
                }
                else
                {
                    Name = chunk.PeekNullPaddedAnsi(0, 8);
                    Short = 0;
                    Long = 0;
                }
            }

            public override string ToString()
            {
                return Name.ToString();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
