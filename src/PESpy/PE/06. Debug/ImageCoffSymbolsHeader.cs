using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public struct ImageCoffSymbolsHeader : IValue, IViewable
    {
        public int NumberOfSymbols => chunk.PeekInt32(0);

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

        public int NumberOfLinenumbers => chunk.PeekInt32(8);
        public int LvaToFirstLinenumber => chunk.PeekInt32(12);
        public int RvaToFirstByteOfCode => chunk.PeekInt32(16);
        public int RvaToLastByteOfCode => chunk.PeekInt32(20);
        public int RvaToFirstByteOfData => chunk.PeekInt32(24);
        public int RvaToLastByteOfData => chunk.PeekInt32(28);

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //NumberOfSymbols
            sizeof(int) + //LvaToFirstSymbol
            sizeof(int) + //NumberOfLinenumbers
            sizeof(int) + //LvaToFirstLinenumber
            sizeof(int) + //RvaToFirstByteOfCode
            sizeof(int) + //RvaToLastByteOfCode
            sizeof(int) + //RvaToFirstByteOfData
            sizeof(int); //RvaToLastByteOfData

        private readonly MemoryChunk chunk;

        internal ImageCoffSymbolsHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            lvaToFirstSymbol = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (LvaToFirstSymbol.IsValid)
                writer.WriteUniqueGlobal(LvaToFirstSymbol.Value);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_COFF_SYMBOLS_HEADER, this, ViewKind.ImageCoffSymbolsHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(NumberOfSymbols), NumberOfSymbols);

            s.WriteField(nameof(LvaToFirstSymbol), LvaToFirstSymbol.ListedOffset);

            s.WriteField(nameof(NumberOfLinenumbers), NumberOfLinenumbers);
            s.WriteField(nameof(LvaToFirstLinenumber), LvaToFirstLinenumber);
            s.WriteField(nameof(RvaToFirstByteOfCode), RvaToFirstByteOfCode);
            s.WriteField(nameof(RvaToLastByteOfCode), RvaToLastByteOfCode);
            s.WriteField(nameof(RvaToFirstByteOfData), RvaToFirstByteOfData);
            s.WriteField(nameof(RvaToLastByteOfData), RvaToLastByteOfData);

            return s.ToArray();
        }
    }
}
