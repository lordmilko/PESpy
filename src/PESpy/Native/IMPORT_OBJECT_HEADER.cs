namespace PESpy.Native
{
    public struct IMPORT_OBJECT_HEADER
    {
        public const short IMPORT_OBJECT_HDR_SIG2 = -1;

        public short Sig1;                       // Must be IMAGE_FILE_MACHINE_UNKNOWN
        public short Sig2;                       // Must be IMPORT_OBJECT_HDR_SIG2.
        public short Version;
        public short Machine;
        public uint TimeDateStamp;              // Time/date stamp
        public int SizeOfData;                 // particularly useful for incremental links

        //union {
        //    short Ordinal;                // if grf & IMPORT_OBJECT_ORDINAL
        //    short Hint;
        //}
        //DUMMYUNIONNAME;

        //short Type : 2;                   // IMPORT_TYPE
        //short NameType : 3;               // IMPORT_NAME_TYPE
        //short Reserved : 11;              // Reserved. Must be zero.
    }
}
