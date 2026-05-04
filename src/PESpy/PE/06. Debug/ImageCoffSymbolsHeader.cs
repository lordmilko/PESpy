using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public struct ImageCoffSymbolsHeader : IValue, IViewable
    {
        private const int NumberOfSymbolsOffset = 0;
        internal const int LvaToFirstSymbolOffset = 4;
        private const int NumberOfLinenumbersOffset = 8;
        private const int LvaToFirstLinenumberOffset = 12;
        private const int RvaToFirstByteOfCodeOffset = 16;
        private const int RvaToLastByteOfCodeOffset = 20;
        private const int RvaToFirstByteOfDataOffset = 24;
        private const int RvaToLastByteOfDataOffset = 28;

        public int NumberOfSymbols => chunk.PeekInt32(NumberOfSymbolsOffset);

        private RVA<CoffSymbolTable> lvaToFirstSymbol;

        public RVA<CoffSymbolTable> LvaToFirstSymbol
        {
            get
            {
                if (lvaToFirstSymbol.ListedOffset == 0)
                {
                    var value = chunk.PeekInt32(LvaToFirstSymbolOffset);

                    //Value is relative to the start of this data
                    var offset = chunk.AbsoluteOffset + value;

                    var valueChunk = new MemoryChunk(chunk.block, offset);

                    var result = new CoffSymbolTable(valueChunk, NumberOfSymbols);

                    lvaToFirstSymbol = new RVA<CoffSymbolTable>(value, offset, result);
                }

                return lvaToFirstSymbol;
            }
        }

        public int NumberOfLinenumbers => chunk.PeekInt32(NumberOfLinenumbersOffset);
        public int LvaToFirstLinenumber => chunk.PeekInt32(LvaToFirstLinenumberOffset);
        public int RvaToFirstByteOfCode => chunk.PeekInt32(RvaToFirstByteOfCodeOffset);
        public int RvaToLastByteOfCode => chunk.PeekInt32(RvaToLastByteOfCodeOffset);
        public int RvaToFirstByteOfData => chunk.PeekInt32(RvaToFirstByteOfDataOffset);
        public int RvaToLastByteOfData => chunk.PeekInt32(RvaToLastByteOfDataOffset);

        public long Offset => chunk.AbsoluteOffset;

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
            writer.WriteUniqueRVAField(LvaToFirstSymbol, Offset, LvaToFirstSymbolOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageCoffSymbolsHeader, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(NumberOfSymbols), NumberOfSymbolsOffset, NumberOfSymbols);
                    break;

                case 1:
                    structWriter.WriteField(nameof(LvaToFirstSymbol), LvaToFirstSymbolOffset, LvaToFirstSymbol.ListedOffset);
                    break;

                case 2:
                    structWriter.WriteField(nameof(NumberOfLinenumbers), NumberOfLinenumbersOffset, NumberOfLinenumbers);
                    break;

                case 3:
                    structWriter.WriteField(nameof(LvaToFirstLinenumber), LvaToFirstLinenumberOffset, LvaToFirstLinenumber);
                    break;

                case 4:
                    structWriter.WriteField(nameof(RvaToFirstByteOfCode), RvaToFirstByteOfCodeOffset, RvaToFirstByteOfCode);
                    break;

                case 5:
                    structWriter.WriteField(nameof(RvaToLastByteOfCode), RvaToLastByteOfCodeOffset, RvaToLastByteOfCode);
                    break;

                case 6:
                    structWriter.WriteField(nameof(RvaToFirstByteOfData), RvaToFirstByteOfDataOffset, RvaToFirstByteOfData);
                    break;

                case 7:
                    structWriter.WriteField(nameof(RvaToLastByteOfData), RvaToLastByteOfDataOffset, RvaToLastByteOfData);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
