namespace PESpy.Native
{
    //https://github.com/dotnet/runtime/blob/e220a94d842524b408c35b381fc326c4159005f0/src/coreclr/debug/inc/runtimeinfo.h#L15

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
