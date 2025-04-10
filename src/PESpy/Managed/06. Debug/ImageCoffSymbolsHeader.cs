using System.Diagnostics;

namespace PESpy
{
    public readonly struct ImageCoffSymbolsHeader : IValue
    {
        public int NumberOfSymbols { get; }
        public RVA<CoffSymbolTable> LvaToFirstSymbol { get; }
        public int NumberOfLinenumbers { get; }
        public int LvaToFirstLinenumber { get; }
        public int RvaToFirstByteOfCode { get; }
        public int RvaToLastByteOfCode { get; }
        public int RvaToFirstByteOfData { get; }
        public int RvaToLastByteOfData { get; }

        public int Offset { get; }

        internal ImageCoffSymbolsHeader(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            NumberOfSymbols = reader.ReadInt32();
            var lvaToFirstSymbol = reader.ReadInt32();
            NumberOfLinenumbers = reader.ReadInt32();
            LvaToFirstLinenumber = reader.ReadInt32();
            RvaToFirstByteOfCode = reader.ReadInt32();
            RvaToLastByteOfCode = reader.ReadInt32();
            RvaToFirstByteOfData = reader.ReadInt32();
            RvaToLastByteOfData = reader.ReadInt32();

            //This offset _should_ be the same offset that was pointed to by the IMAGE_FILE_HEADER

            if (lvaToFirstSymbol > 0)
            {
                CoffSymbolTable symbolTable;

                var symbolTableOffset = Offset + lvaToFirstSymbol;

                if (peFile.FileHeader.PointerToSymbolTable.IsValid && peFile.FileHeader.PointerToSymbolTable.ListedAddress == symbolTableOffset && NumberOfSymbols == peFile.FileHeader.NumberOfSymbols)
                    symbolTable = peFile.FileHeader.PointerToSymbolTable.Value;
                else
                {
                    reader.Seek(symbolTableOffset);

                    symbolTable = new CoffSymbolTable(reader, NumberOfSymbols);
                }

                LvaToFirstSymbol = new RVA<CoffSymbolTable>(lvaToFirstSymbol, symbolTableOffset, symbolTable);
            }
            else
                LvaToFirstSymbol = default;

            if (LvaToFirstLinenumber > 0)
            {
                var lineNumberOffset = Offset + LvaToFirstLinenumber;

                Debug.Assert(false);
            }
        }
    }
}
