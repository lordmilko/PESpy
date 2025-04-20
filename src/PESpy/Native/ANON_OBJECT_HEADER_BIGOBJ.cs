using System;

namespace PESpy.Native
{
    internal struct ANON_OBJECT_HEADER_BIGOBJ
    {
        /* same as ANON_OBJECT_HEADER_V2 */
        public short Sig1;            // Must be IMAGE_FILE_MACHINE_UNKNOWN
        public short Sig2;            // Must be 0xffff
        public short Version;         // >= 2 (implies the Flags field is present)
        public short Machine;         // Actual machine - IMAGE_FILE_MACHINE_xxx
        public uint TimeDateStamp;
        public Guid ClassID;         // {D1BAA1C7-BAEE-4ba9-AF20-FAF66AA4DCB8}
        public int SizeOfData;      // Size of data that follows the header
        public int Flags;           // 0x1 -> contains metadata
        public int MetaDataSize;    // Size of CLR metadata
        public int MetaDataOffset;  // Offset of CLR metadata

        /* bigobj specifics */
        public int NumberOfSections; // extended from WORD
        public int PointerToSymbolTable;
        public int NumberOfSymbols;
    }
}