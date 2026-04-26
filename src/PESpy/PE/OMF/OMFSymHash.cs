using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /* There is an enum type OMFHash that contains the following values
     *     OMFHASH_NONE    (0) - no hashing
     *     OMFHASH_SUMUC16 (1) - upper case sum of chars in 16 bit table
     *     OMFHASH_SUMUC32 (2) - upper case sum of chars in 32 bit table
     *     OMFHASH_ADDR16  (3) - sorted by increasing address in 16 bit table
     *     OMFHASH_ADDR32  (4) - sorted by increasing address in 32 bit table
     *
     * In the OMFHashedSymbols synthetic type, there are four components
     * - OMFHashSym
     * - SymTypeList
     * - Symbol Hash Table
     * - Address Hash Table
     *
     * The format of the Symbol and Address Hash tables depends on the type of value present in symhash and addrhash.
     *
     * In dumpsym7.cpp the following hash kinds are described, with a parsing implementation shown
     *
     * Value | Used By           | Implementation | Description |
     * ------|-------------------|----------------|-------------|
     * 0     | symhash, addrhash | N/A            | No hashing
     * 1     | symhash, addrhash |                | Sum of bytes, 16 bit addressing
     * 2     | symhash           | SymHash32      | Sum of bytes, 32 bit addressing
     * 3     | addrhash          |                | seg :off sort, 16 bit addressing
     * 4     | addrhash          | AddrHash32     | seg :off sort, 32 bit addressing
     * 5     | symhash           | Addrhash32     | Shifted sum of bytes, 16 bit addressing
     * 5     | addrhash          |                | seg :off sort, 32 bit addressing - 32 bit aligned
     * 6     | symhash           | SymHash32      | Shifted sum of bytes, 32 bit addressing
     * 7     | addrhash          |                | Modified seg :off sort, 16 bit addressing
     * 8     | addrhash          | Addrhash32NB09 | Modified seg :off sort, 32 bit addressing
     * 10    | symhash           | SymHash32Long  | Xor shift of drwords (MSC 8) 32-bit addressing
     * 12    | saddrhash         | AddrHash32     | seg :off grouped sort, 32 bit addressing - 32 bit aligned
     *
     * On PDF page 85 of the spec (https://web.archive.org/web/20160909082838/http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf)
     * in section 7.5 it describes the format of the Symbol and Address Hash tables that would be present when symhash == 10 and addrhash == 12
     *
     * I can sort of see how the OMFHASH enum might marry up with the functions shown in dumpsym7.cpp
     */

    [Source(SourceKind.cvexefmt_h)]
    public readonly struct OMFSymHash : IValue, IViewable
    {
        private const int symhashOffset = 0;
        private const int addrhashOffset = 2;
        private const int cbSymbolOffset = 4;
        private const int cbHSymOffset = 8;
        private const int cbHAddrOffset = 12;

        public ushort symhash => chunk.PeekUInt16(symhashOffset);

        public ushort addrhash => chunk.PeekUInt16(addrhashOffset);

        public int cbSymbol => chunk.PeekInt32(cbSymbolOffset);

        public int cbHSym => chunk.PeekInt32(cbHSymOffset);

        public int cbHAddr => chunk.PeekInt32(cbHAddrOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //symhash
            sizeof(ushort) + //addrhash
            sizeof(int) + //cbSymbol
            sizeof(int) + //cbHSym
            sizeof(int); //cbHAddr

        private readonly MemoryChunk chunk;

        internal OMFSymHash(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.OMFSymHash, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(symhash), symhashOffset, symhash);
                    break;

                case 1:
                    structWriter.WriteField(nameof(addrhash), addrhashOffset, addrhash);
                    break;

                case 2:
                    structWriter.WriteField(nameof(cbSymbol), cbSymbolOffset, cbSymbol);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbHSym), cbHSymOffset, cbHSym);
                    break;

                case 4:
                    structWriter.WriteField(nameof(cbHAddr), cbHAddrOffset, cbHAddr);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
