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
        public ImageSymbol[] Symbols { get; }

        public RawValue<string>[] Strings { get; }

        public int Offset { get; }

#if PEFAST
        private readonly MemoryChunk chunk;

        internal CoffSymbolTable(in MemoryChunk chunk, int numberOfSymbols)
        {
            this.chunk = chunk;
        }
#else
        internal CoffSymbolTable(IFileReader reader, int numberOfSymbols)
        {
            Offset = (int) reader.Position;

            //Can't find any info on when IMAGE_SYMBOL_EX should be used instead of IMAGE_SYMBOL   

            var symbols = new List<ImageSymbol>();

            //There may be aux symbols (which are also 18 bytes large)

            var end = reader.Position + (ImageSymbol.StructSize * numberOfSymbols);

            while (reader.Position < end)
                symbols.Add(new ImageSymbol(reader));

            Symbols = symbols.ToArray();

            //Next comes the string table

            var start = reader.Position;

            var stringTableSize = reader.ReadInt32();

            end = start + stringTableSize;

            var strings = new List<RawValue<string>>();

            while (reader.Position < end)
            {
                var offset = (int) reader.Position;
                var str = reader.ReadAnsiNullTerminatedString();
                strings.Add(new RawValue<string>(offset, str));
            }

            Strings = strings.ToArray();

            Debug.Assert(reader.Position == end);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
