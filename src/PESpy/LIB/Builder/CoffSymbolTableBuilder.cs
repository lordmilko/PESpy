using System;
using System.Collections.Generic;

namespace PESpy
{
    internal class CoffSymbolTableBuilder
    {
        public List<ImageSymbolBuilder> Symbols { get; }

        //Not all strings are actually referenced by symbols
        public List<string> Strings { get; }

        public CoffSymbolTableBuilder(CoffSymbolTable coffSymbolTable)
        {
            Symbols = new List<ImageSymbolBuilder>(coffSymbolTable.Symbols.Length);

            foreach (var symbol in coffSymbolTable.Symbols)
                Symbols.Add(new ImageSymbolBuilder(symbol));

            Strings = new List<string>(coffSymbolTable.Strings.Length);

            foreach (var str in coffSymbolTable.Strings)
                Strings.Add(str.Value.ToString());
        }

        public void WriteTo(FileWriter writer, int numberOfSymbols)
        {
            //Before we write the symbols, we need to write the string table
            //so we know the offsets of all of the long name symbols we need to write

            var start = writer.Position;

            writer.Skip(numberOfSymbols * ImageSymbol.StructSize);

            //Make space for the String Table Size
            var stringTableSizeOffset = writer.Position;
            writer.Skip(sizeof(int));

            var nameOffsetMap = new Dictionary<string, int>();

            //Not all strings are actually referenced by symbols, so we need to store them directly outside of the symbols
            foreach (var str in Strings)
            {
                //If the name is more than 8 bytes long, we need to store it in the symbol table
                if (str.Length > 8)
                {
                    nameOffsetMap[str] = writer.Position - stringTableSizeOffset;
                }

                writer.WriteNullTerminatedAnsiString(str);
            }

            var end = writer.Position;

            //Now let's actually write the symbols

            writer.Seek(start);

            foreach (var symbol in Symbols)
                symbol.WriteTo(writer, nameOffsetMap);

            //And finally, write the string table size.
            //Note that the size includes the size of the strings _plus_ the size of this field itself
            var size = end - stringTableSizeOffset;
            writer.Seek(stringTableSizeOffset);
            writer.WriteUInt32((uint) size);

            writer.Seek(end);
        }
    }
}
