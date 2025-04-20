using System;

namespace PESpy.Native
{
    internal struct ANON_OBJECT_HEADER
    {
        public short Sig1;            // Must be IMAGE_FILE_MACHINE_UNKNOWN
        public short Sig2;            // Must be 0xffff
        public short Version;         // >= 1 (implies the CLSID field is present)
        public short Machine;
        public uint TimeDateStamp;
        public Guid ClassID;         // Used to invoke CoCreateInstance
        public int SizeOfData;      // Size of data that follows the header
    }
}
