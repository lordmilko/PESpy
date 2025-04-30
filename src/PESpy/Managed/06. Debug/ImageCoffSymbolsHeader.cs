using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public struct ImageCoffSymbolsHeader : IValue, IViewable
    {
#if PEFAST
        public int NumberOfSymbols => chunk.PeekInt32(0);
#else
        public int NumberOfSymbols { get; }
#endif
#if PEFAST
        private RVA<CoffSymbolTable> lvaToFirstSymbol;

        public RVA<CoffSymbolTable> LvaToFirstSymbol
        {
            get
            {
                if (lvaToFirstSymbol.ListedOffset == 0)
                {
                    var value = chunk.PeekInt32(4);

                    //Value is relative to the start of this data
                    var offset = chunk.AbsoluteOffset + value;

                    var valueChunk = new MemoryChunk(chunk.block, offset);

                    var result = new CoffSymbolTable(valueChunk, NumberOfSymbols);

                    lvaToFirstSymbol = new RVA<CoffSymbolTable>(value, offset, result);
                }

                return lvaToFirstSymbol;
            }
        }
#else
        public RVA<CoffSymbolTable> LvaToFirstSymbol { get; }
#endif
#if PEFAST
        public int NumberOfLinenumbers => chunk.PeekInt32(8);
#else
        public int NumberOfLinenumbers { get; }
#endif
#if PEFAST
        public int LvaToFirstLinenumber => chunk.PeekInt32(12);
#else
        public int LvaToFirstLinenumber { get; }
#endif
#if PEFAST
        public int RvaToFirstByteOfCode => chunk.PeekInt32(16);
#else
        public int RvaToFirstByteOfCode { get; }
#endif
#if PEFAST
        public int RvaToLastByteOfCode => chunk.PeekInt32(20);
#else
        public int RvaToLastByteOfCode { get; }
#endif
#if PEFAST
        public int RvaToFirstByteOfData => chunk.PeekInt32(24);
#else
        public int RvaToFirstByteOfData { get; }
#endif
#if PEFAST
        public int RvaToLastByteOfData => chunk.PeekInt32(28);
#else
        public int RvaToLastByteOfData { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageCoffSymbolsHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            lvaToFirstSymbol = default;
        }
#else
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
#endif

        void IViewable.WriteView(PESpy.View.ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_COFF_SYMBOLS_HEADER), this, ViewKind.ImageCoffSymbolsHeader);

            s.WriteField(nameof(NumberOfSymbols), NumberOfSymbols);

            s.WriteField(nameof(LvaToFirstSymbol), LvaToFirstSymbol.ListedOffset);

            if (LvaToFirstSymbol.IsValid)
                writer.WriteUniqueGlobal(LvaToFirstSymbol.Value);

            s.WriteField(nameof(NumberOfLinenumbers), NumberOfLinenumbers);
            s.WriteField(nameof(LvaToFirstLinenumber), LvaToFirstLinenumber);
            s.WriteField(nameof(RvaToFirstByteOfCode), RvaToFirstByteOfCode);
            s.WriteField(nameof(RvaToLastByteOfCode), RvaToLastByteOfCode);
            s.WriteField(nameof(RvaToFirstByteOfData), RvaToFirstByteOfData);
            s.WriteField(nameof(RvaToLastByteOfData), RvaToLastByteOfData);
        }
    }
}
