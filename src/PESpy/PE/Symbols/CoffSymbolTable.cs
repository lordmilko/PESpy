using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Encapsulates the components of the COFF Symbol Table.<para/>
    /// This type does not have a native struct definition.
    /// </summary>
    public class CoffSymbolTable : IValue, IViewable //Class so that it can be reused with IMAGE_DEBUG_TYPE_COFF
    {
#if PEFAST
        private ImageSymbol[]? symbols;

        public ImageSymbol[] Symbols
        {
            get
            {
                if (symbols == null)
                {
                    /* Can't find any info on when IMAGE_SYMBOL_EX should be used instead of IMAGE_SYMBOL.
                     * Only difference seems to be SHORT SectionNumber vs LONG SectionNumber. Maybe if you have
                     * more than 32767 sections you're meant to use IMAGE_SYMBOL_EX?
                     *
                     * NumberOfSymbols gives us the total number of regular and AUX symbols. We don't
                     * know how many AUX symbols we will have; all we can know is how much memory the symbols
                     * will occupy (both IMAGE_SYMBOL and IMAGE_AUX_SYMBOL are 18 bytes) */
                    using var results = new PooledList<ImageSymbol>();

                    var read = 0;
                    var end = numberOfSymbols * ImageSymbol.StructSize;

                    while (read < end)
                    {
                        var symbol = new ImageSymbol(chunk.Slice(read), this);

                        read += ImageSymbol.StructSize + (symbol.NumberOfAuxSymbols * ImageAuxSymbol.StructSize);

                        results.Add(symbol);
                    }

                    symbols = results.ToArray();
                }

                return symbols;
            }
        }

        //I believe that the StringTableSize describes the size of the string table, in bytes, _including the size of this value_
        public int StringTableSize => chunk.PeekInt32(numberOfSymbols * ImageSymbol.StructSize);

        private RawValue<AnsiString>[]? strings;

        public RawValue<AnsiString>[] Strings
        {
            get
            {
                if (strings == null)
                {
                    //NumberOfSymbols includes AUX symbols as well
                    var offset = numberOfSymbols * ImageSymbol.StructSize;

                    var stringTableSize = chunk.PeekInt32(offset);

                    //Note that the size of the string table itself seems to be included in its size
                    var end = offset + stringTableSize;

                    offset += 4;

                    if (offset == end)
                        strings = Array.Empty<RawValue<AnsiString>>();
                    else
                    {
                        using var results = new PooledList<RawValue<AnsiString>>();

                        while (offset < end)
                        {
                            var str = chunk.PeekAnsiNullTerminatedString(offset);
                            results.Add(new RawValue<AnsiString>(chunk.AbsoluteOffset + offset, str));

                            var length = str.Length + 1;

                            offset += length;
                        }

                        strings = results.ToArray();
                    }
                }

                return strings;
            }
        }

        public AnsiString GetString(int offset)
        {
            //The string table begins with the StringTableSize. So an offset of 4 targets the first string after the offset
            return chunk.PeekAnsiNullTerminatedString((numberOfSymbols * ImageSymbol.StructSize) + offset);
        }

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            numberOfSymbols * ImageSymbol.StructSize + //Will include regular and aux symbols
            StringTableSize; //StringTableSize includes its own length (4) in its total size

        private readonly MemoryChunk chunk;
        private readonly int numberOfSymbols;

        internal CoffSymbolTable(in MemoryChunk chunk, int numberOfSymbols)
        {
            this.chunk = chunk;
            this.numberOfSymbols = numberOfSymbols;

#if STRESS_TEST
            _ = Symbols;
            _ = Strings;
#endif
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(PESpy.Strings.CoffSymbolTable, this, ViewKind.CoffSymbolTable, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline(Symbols);
            s.WriteField("String Table Size", StringTableSize);
            s.WriteInlineAnsiNullTerminated(Strings);

            return s.ToArray();
        }
    }
}
