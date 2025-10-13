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

    [Source(SourceKind.cvexefmt)]
    public readonly struct OMFSymHash : IValue, IViewable
    {
        public ushort symhash => chunk.PeekUInt16(0);

        public ushort addrhash => chunk.PeekUInt16(2);

        public int cbSymbol => chunk.PeekInt32(4);

        public int cbHSym => chunk.PeekInt32(8);

        public int cbHAddr => chunk.PeekInt32(12);

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
            writer.NewStruct(Strings.OMFSymHash, this, ViewKind.OMFSymHash, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(symhash), symhash);
            s.WriteField(nameof(addrhash), addrhash);
            s.WriteField(nameof(cbSymbol), cbSymbol);
            s.WriteField(nameof(cbHSym), cbHSym);
            s.WriteField(nameof(cbHAddr), cbHAddr);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
