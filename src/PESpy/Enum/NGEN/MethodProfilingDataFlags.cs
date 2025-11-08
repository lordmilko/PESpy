namespace PESpy
{
    public enum MethodProfilingDataFlags
    {
        // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
        ReadMethodCode = 0,  // 0x00001  // Also means the method was executed
        ReadMethodDesc = 1,  // 0x00002
        RunOnceMethod = 2,  // 0x00004
        RunNeverMethod = 3,  // 0x00008
                             //  MethodStoredDataAccess        = 4,  // 0x00010  // obsolete
        WriteMethodDesc = 5,  // 0x00020
                              //  ReadFCallHash                 = 6,  // 0x00040  // obsolete
        ReadGCInfo = 7,  // 0x00080
        CommonReadGCInfo = 8,  // 0x00100
                               //  ReadMethodDefRidMap           = 9,  // 0x00200  // obsolete
        ReadCerMethodList = 10, // 0x00400
        ReadMethodPrecode = 11, // 0x00800
        WriteMethodPrecode = 12, // 0x01000
        ExcludeHotMethodCode = 13, // 0x02000  // Hot method should be excluded from the ReadyToRun image
        ExcludeColdMethodCode = 14, // 0x04000  // Cold method should be excluded from the ReadyToRun image
        DisableInlining = 15, // 0x08000  // Disable inlining of this method in optimized AOT native code
    }
}
