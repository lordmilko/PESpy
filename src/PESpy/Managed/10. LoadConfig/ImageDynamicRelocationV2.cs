using System;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageDynamicRelocationV2 : IValue
    {
        public int HeaderSize { get; }

        public int FixupInfoSize { get; }

        public long Symbol { get; }

        public int SymbolGroup { get; }

        public int Flags { get; }

        public int Offset { get; }

        internal ImageDynamicRelocationV2(ref FileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            HeaderSize = reader.ReadInt32();
            FixupInfoSize = reader.ReadInt32();
            Symbol = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadInt64();
            SymbolGroup = reader.ReadInt32();
            Flags = reader.ReadInt32(); //todo: which enum

            // ...     variable length header fields
            // BYTE    FixupInfo[FixupInfoSize]
            throw new NotImplementedException();
        }
    }
}
