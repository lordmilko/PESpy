using System;

namespace PESpy.Native
{
    internal struct ANON_OBJECT_HEADER_V2
    {
        public short Sig1;            // Must be IMAGE_FILE_MACHINE_UNKNOWN
        public short Sig2;            // Must be 0xffff
        public short Version;         // >= 2 (implies the Flags field is present - otherwise V1)
        public short Machine;
        public uint TimeDateStamp;
        public Guid ClassID;         // Used to invoke CoCreateInstance
        public int SizeOfData;      // Size of data that follows the header
        public int Flags;           // 0x1 -> contains metadata
        public int MetaDataSize;    // Size of CLR metadata
        public int MetaDataOffset;  // Offset of CLR metadata
    }
}