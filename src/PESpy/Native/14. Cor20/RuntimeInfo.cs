namespace PESpy.Native
{
    //.NET Single File runtime info
    internal unsafe struct RuntimeInfo
    {
        public fixed byte Signature[18];
        public int Version;
        public fixed byte RuntimeModuleIndex[24];
        public fixed byte DacModuleIndex[24];
        public fixed byte DbiModuleIndex[24];

        //Version 2 only
        public fixed int RuntimeVersion[4];
    }
}
