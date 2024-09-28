using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageLoadConfigDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_LOAD_CONFIG_DIRECTORY64
    {
        public int Size;
        public int TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public int GlobalFlagsClear; //Flags
        public int GlobalFlagsSet; //Flags
        public int CriticalSectionDefaultTimeout;
        public long DeCommitFreeBlockThreshold;
        public long DeCommitTotalFreeThreshold;
        public long LockPrefixTable;
        public long MaximumAllocationSize;
        public long VirtualMemoryThreshold; //Flags
        public long ProcessAffinityMask; //Don't think this is flags
        public int ProcessHeapFlags; //Flags
        public ushort CSDVersion;
        public ushort DependentLoadFlags; //Flags
        public long EditList;
        public long SecurityCookie;
        public long SEHandlerTable;
        public long SEHandlerCount;

        //Windows SDK 8.1+
        public long GuardCFCheckFunctionPointer;
        public long GuardCFDispatchFunctionPointer;
        public long GuardCFFunctionTable;
        public long GuardCFFunctionCount;
        public IMAGE_GUARD GuardFlags;

        //Windows SDK 10.0.10586.0+
        public IMAGE_LOAD_CONFIG_CODE_INTEGRITY CodeIntegrity;
        public long GuardAddressTakenIatEntryTable;
        public long GuardAddressTakenIatEntryCount;
        public long GuardLongJumpTargetTable;
        public long GuardLongJumpTargetCount;
        public long DynamicValueRelocTable;
        public long CHPEMetadataPointer;

        //Windows SDK 10.0.15063.468+
        public long GuardRFFailureRoutine;
        public long GuardRFFailureRoutineFunctionPointer;
        public int DynamicValueRelocTableOffset;
        public ushort DynamicValueRelocTableSection;
        public ushort Reserved2;
        public long GuardRFVerifyStackPointerFunctionPointer;
        public int HotPatchTableOffset;
        public int Reserved3;
        public long EnclaveConfigurationPointer;
        public long VolatileMetadataPointer;
        public long GuardEHContinuationTable;
        public long GuardEHContinuationCount;
        public long GuardXFGCheckFunctionPointer;
        public long GuardXFGDispatchFunctionPointer;
        public long GuardXFGTableDispatchFunctionPointer;
        public long CastGuardOsDeterminedFailureMode;
        public long GuardMemcpyFunctionPointer;
    }
}