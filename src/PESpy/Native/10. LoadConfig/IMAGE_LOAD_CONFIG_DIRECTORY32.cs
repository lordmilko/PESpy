using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageLoadConfigDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_LOAD_CONFIG_DIRECTORY32
    {
        public int Size;
        public int TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public int GlobalFlagsClear; //Flags
        public int GlobalFlagsSet; //Flags
        public int CriticalSectionDefaultTimeout;
        public int DeCommitFreeBlockThreshold;
        public int DeCommitTotalFreeThreshold;
        public int LockPrefixTable;
        public int MaximumAllocationSize;
        public int VirtualMemoryThreshold;
        public int ProcessHeapFlags; //Flags
        public int ProcessAffinityMask; //Don't think this is flags
        public ushort CSDVersion;
        public ushort DependentLoadFlags; //Flags
        public int EditList;
        public int SecurityCookie;
        public int SEHandlerTable;
        public int SEHandlerCount;

        //Windows SDK 8.1+
        public int GuardCFCheckFunctionPointer;
        public int GuardCFDispatchFunctionPointer;
        public int GuardCFFunctionTable;
        public int GuardCFFunctionCount;
        public IMAGE_GUARD GuardFlags;

        //Windows SDK 10.0.10586.0+
        public IMAGE_LOAD_CONFIG_CODE_INTEGRITY CodeIntegrity;
        public int GuardAddressTakenIatEntryTable;
        public int GuardAddressTakenIatEntryCount;
        public int GuardLongJumpTargetTable;
        public int GuardLongJumpTargetCount;
        public int DynamicValueRelocTable;
        public int CHPEMetadataPointer;

        ////Windows SDK 10.0.15063.468+
        public int GuardRFFailureRoutine;
        public int GuardRFFailureRoutineFunctionPointer;
        public int DynamicValueRelocTableOffset;
        public ushort DynamicValueRelocTableSection;
        public ushort Reserved2;
        public int GuardRFVerifyStackPointerFunctionPointer;
        public int HotPatchTableOffset;
        public int Reserved3;
        public int EnclaveConfigurationPointer;
        public int VolatileMetadataPointer;
        public int GuardEHContinuationTable;
        public int GuardEHContinuationCount;
        public int GuardXFGCheckFunctionPointer;
        public int GuardXFGDispatchFunctionPointer;
        public int GuardXFGTableDispatchFunctionPointer;
        public int CastGuardOsDeterminedFailureMode;
        public int GuardMemcpyFunctionPointer;
    }
}