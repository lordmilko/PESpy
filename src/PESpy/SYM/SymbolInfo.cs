using PESpy.View;

namespace PESpy.SYM
{
    //Type is made up
    public struct SymbolInfo
    {
        private const int CBOFFSET = 2;
        private const int CBOFFSET_BIG = 3;

        public RawValue<int[]> SymbolOffsets { get; }

        public symdef16_s[] Symbols16 { get; }

        public symdef_s[] Symbols32 { get; }

        //The offsets to the symbols and the symbols themselves are relative to the offset of the segdef_s
        internal SymbolInfo(
            in MemoryChunk chunk,
            MSF symType,
            int symBase,
            int numSyms,
            bool isSeg)
        {
            //gd_psymoff points to an array of _offsets_ to symbol records. These offsets are
            //2 bytes large when not using big symbols, and 3 bytes large when you are using big symbols

            int arrayOffset;
            int bytesPerOffset;

            //from symres GetNameFromAddr in XP
            if ((symType & MSF.MSF_BIGSYMDEF) != 0)
            {
                if (isSeg)
                {
                    //When there's a large number of symbols, gd_psymoff is a distance in paragraphs
                    arrayOffset = symBase * 16; //The alignment here is fixed at 16; the alignment constants in the MSF symType don't matter
                }
                else
                {
                    //Otherwise, we're just reading absolute symbols at the start of the file
                    arrayOffset = symBase;
                }

                bytesPerOffset = CBOFFSET_BIG;
            }
            else
            {
                arrayOffset = symBase;

                bytesPerOffset = CBOFFSET;
            }

            //Read the offsets of each symbol

            var symbolOffsetsChunk = chunk.Slice(arrayOffset);

            var symbolOffsets = new int[numSyms];

            for (var i = 0; i < symbolOffsets.Length; i++)
            {
                var bytes = symbolOffsetsChunk.PeekNativeSpan<byte>(i * bytesPerOffset, bytesPerOffset);

                if (bytesPerOffset == CBOFFSET)
                    symbolOffsets[i] = bytes[0] | (bytes[1] << 8);
                else
                    symbolOffsets[i] = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16);
            }

            //Read the actual symbols themselves
            symdef16_s[] symbols16 = null;
            symdef_s[] symbols32 = null;

            if ((symType & MSF.MSF_32BITSYMS) != 0)
            {
                symbols32 = new symdef_s[numSyms];

                for (var i = 0; i < symbols32.Length; i++)
                    symbols32[i] = new symdef_s(chunk.Slice(symbolOffsets[i]));
            }
            else
            {
                symbols16 = new symdef16_s[numSyms];

                for (var i = 0; i < symbols16.Length; i++)
                    symbols16[i] = new symdef16_s(chunk.Slice(symbolOffsets[i]));
            }

            SymbolOffsets = new RawValue<int[]>(symbolOffsetsChunk.AbsoluteOffset, symbolOffsets);
            Symbols16 = symbols16;
            Symbols32 = symbols32;
        }

        internal void WriteGlobals(ViewWriter viewWriter)
        {
            if (Symbols16 != null)
            {
                var offsetsSize = SymbolOffsets.Value.Length * CBOFFSET;
                viewWriter.WriteGlobal(SymbolOffsets.Offset, SymbolOffsets.Value, offsetsSize, ViewKind.SymbolOffsets);
                viewWriter.WriteGlobal(Symbols16);
            }
            else
            {
                var offsetsSize = SymbolOffsets.Value.Length * CBOFFSET_BIG;
                viewWriter.WriteGlobal(SymbolOffsets.Offset, SymbolOffsets.Value, offsetsSize, ViewKind.SymbolOffsets);
                viewWriter.WriteGlobal(Symbols32);
            }
        }
    }
}
