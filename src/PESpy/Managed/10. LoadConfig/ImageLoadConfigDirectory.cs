using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_LOAD_CONFIG_DIRECTORY32"/> / <see cref="IMAGE_LOAD_CONFIG_DIRECTORY64"/> structure.
    /// </summary>
    public class ImageLoadConfigDirectory : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
    {
        public const int CF_FUNCTION_TABLE_SIZE_SHIFT = 28;

        public const int IMAGE_ENCLAVE_SHORT_ID_LENGTH = 16;
        public const int IMAGE_ENCLAVE_LONG_ID_LENGTH = 32;

        /// <summary>
        /// The size of the structure. For Windows XP, the size must be specified as 64 for x86 images.
        /// </summary>
        public int Size { get; init; } //0

        #region Default

        /// <summary>
        /// Date and time stamp value. The value is represented in the number of seconds that have elapsed since midnight
        /// (00:00:00), January 1, 1970, Universal Coordinated Time, according to the system clock. The time stamp can be
        /// printed by using the C runtime (CRT) time function.
        /// </summary>
        public uint TimeDateStamp { get; init; } //1

        /// <summary>
        /// Major version number.
        /// </summary>
        public ushort MajorVersion { get; init; } //2

        /// <summary>
        /// Minor version number.
        /// </summary>
        public ushort MinorVersion { get; init; } //3

        /// <summary>
        /// The global loader flags to clear for this process as the loader starts the process.
        /// </summary>
        public int GlobalFlagsClear { get; init; } //4, Flags

        /// <summary>
        /// The global loader flags to set for this process as the loader starts the process.
        /// </summary>
        public int GlobalFlagsSet { get; init; } //5, Flags

        /// <summary>
        /// The default timeout value to use for this process's critical sections that are abandoned.
        /// </summary>
        public int CriticalSectionDefaultTimeout { get; init; } //6

        /// <summary>
        /// Memory that must be freed before it is returned to the system, in bytes.
        /// </summary>
        public long DeCommitFreeBlockThreshold { get; init; } //7

        /// <summary>
        /// Total amount of free memory, in bytes.
        /// </summary>
        public long DeCommitTotalFreeThreshold { get; init; } //8

        /// <summary>
        /// [x86 only] The VA of a list of addresses where the LOCK prefix is used so that they can be replaced with NOP
        /// on single processor machines.
        /// </summary>
        public long LockPrefixTable { get; init; } //9

        /// <summary>
        /// Maximum allocation size, in bytes.
        /// </summary>
        public long MaximumAllocationSize { get; init; } //10

        /// <summary>
        /// Maximum virtual memory size, in bytes.
        /// </summary>
        public long VirtualMemoryThreshold { get; init; } //11, Flags

        /// <summary>
        /// Setting this field to a non-zero value is equivalent to calling SetProcessAffinityMask with this value
        /// during process startup (.exe only)
        /// </summary>
        public long ProcessAffinityMask { get; init; } //12, Don't think this is flags

        /// <summary>
        /// Process heap flags that correspond to the first argument of the HeapCreate function. These flags apply to the
        /// process heap that is created during process startup.
        /// </summary>
        public int ProcessHeapFlags { get; init; } //13, Flags

        /// <summary>
        /// The service pack version identifier.
        /// </summary>
        public ushort CSDVersion { get; init; } //14

        /// <summary>
        /// The default load flags used when the operating system resolves the statically linked imports of a module.
        /// </summary>
        public ushort DependentLoadFlags { get; init; } //15

        /// <summary>
        /// Reserved for use by the system.
        /// </summary>
        public long EditList { get; init; } //16

        /// <summary>
        /// A pointer to a cookie that is used by Visual C++ or GS implementation.
        /// </summary>
        public VA<ulong> SecurityCookie { get; init; } //17

        /// <summary>
        /// [x86 only] The VA of the sorted table of RVAs of each valid, unique SE handler in the image.
        /// </summary>
        public VA<long[]> SEHandlerTable { get; init; } //18

        /// <summary>
        /// [x86 only] The count of unique handlers in the table.
        /// </summary>
        public long SEHandlerCount { get; init; } //19

        #endregion
        #region Windows SDK 8.1+

        /// <summary>
        /// The VA where Control Flow Guard check-function pointer is stored.
        /// </summary>
        public VA<long> GuardCFCheckFunctionPointer { get; init; } //20

        /// <summary>
        /// The VA where Control Flow Guard dispatch-function pointer is stored.
        /// </summary>
        public VA<long> GuardCFDispatchFunctionPointer { get; init; } //21

        /// <summary>
        /// The VA of the sorted table of RVAs of each Control Flow Guard function in the image.
        /// </summary>
        public VA<GuardCFFunctionTable> GuardCFFunctionTable { get; init; } //22

        /// <summary>
        /// The count of unique RVAs in the above table.
        /// </summary>
        public long GuardCFFunctionCount { get; init; } //23

        /// <summary>
        /// Control Flow Guard related flags.
        /// </summary>
        public IMAGE_GUARD GuardFlags { get; init; } //24

        #endregion
        #region Windows SDK 10.0.10586.0+

        /// <summary>
        /// Code integrity information.
        /// </summary>
        public ImageLoadConfigCodeIntegrity CodeIntegrity { get; init; } //25

        /// <summary>
        /// The VA where Control Flow Guard address taken IAT table is stored.
        /// </summary>
        public VA<GuardAddressTakenIatEntryTable> GuardAddressTakenIatEntryTable { get; init; } //26

        /// <summary>
        /// The count of unique RVAs in the above table.
        /// </summary>
        public long GuardAddressTakenIatEntryCount { get; init; } //27

        /// <summary>
        /// The VA where Control Flow Guard long jump target table is stored.
        /// </summary>
        public VA<GuardLongJumpTargetTable> GuardLongJumpTargetTable { get; init; } //28

        /// <summary>
        /// The count of unique RVAs in the above table.
        /// </summary>
        public long GuardLongJumpTargetCount { get; init; } //29

        public long DynamicValueRelocTable { get; init; } //30

        public long CHPEMetadataPointer { get; init; } //31

        #endregion
        #region Windows SDK 10.0.15063.468+

        public long GuardRFFailureRoutine { get; init; } //32, This points straight to a function; there is no function pointer we have to read first

        public VA<long> GuardRFFailureRoutineFunctionPointer { get; init; } //33

        public RVA<ImageDynamicRelocationTable> DynamicValueRelocTableOffset { get; init; } //34

        public ushort DynamicValueRelocTableSection { get; init; } //35

        public ushort Reserved2 { get; init; } //36

        public VA<long> GuardRFVerifyStackPointerFunctionPointer { get; init; } //37

        public int HotPatchTableOffset { get; init; } //38

        public int Reserved3 { get; init; } //39

        public VA<ImageEnclaveConfig> EnclaveConfigurationPointer { get; init; } //40

        //Each successive property from here may or not be present; there is no clear delineation between when each item was added

        public long VolatileMetadataPointer { get; init; } //41

        public VA<GuardEHContinuationTable> GuardEHContinuationTable { get; init; } //42

        public long GuardEHContinuationCount { get; init; } //43

        public VA<long> GuardXFGCheckFunctionPointer { get; init; } //44

        public VA<long> GuardXFGDispatchFunctionPointer { get; init; } //45

        public VA<long> GuardXFGTableDispatchFunctionPointer { get; init; } //46

        public long CastGuardOsDeterminedFailureMode { get; init; } //47

        #endregion
        #region Windows SDK 10.0.22621+

        public VA<long> GuardMemcpyFunctionPointer { get; init; } //48

        #endregion

        public byte[]? UnknownBytes { get; init; }

        public RawOffset Offset { get; }

        internal ImageLoadConfigDirectory(IFileReader reader, PEFile peFile)
        {
            #region Init

            Offset = (RawOffset) reader.Position;
            var is32Bit = peFile.OptionalHeader.Magic == PEMagic.PE32;

            var start = reader.Position;

            Size = reader.ReadInt32();

            var end = start + Size;

            //Exclude Size since we already read it
            reader.FillBuffer(Size - sizeof(int));

            //LockPrefixTable
            //EditList
            long securityCookie = 0;
            long sehandlerTable = 0;
            long guardCFCheckFunctionPointer = 0;
            long guardCFDispatchFunctionPointer = 0;
            long guardCFFunctionTable = 0;
            long guardAddressTakenIatEntryTable = 0;
            long guardLongJumpTargetTable = 0;
            //DynamicValueRelocTable - see notes below regarding how this potentially differs from DynamicValueRelocTableOffset+DynamicValueRelocTableSection
            //CHPEMetadataPointer - I think this is: IMAGE_CHPE_RANGE_ENTRY?
            long guardRFFailureRoutineFunctionPointer = 0;

            int dynamicValueRelocTableOffset = 0;

            long guardRFVerifyStackPointerFunctionPointer = 0;
            //HotPatchTableOffset
            long enclaveConfigurationPointer = 0;
            //VolatileMetadataPointer
            long guardEHContinuationTable = 0;
            long guardXFGCheckFunctionPointer = 0;
            long guardXFGDispatchFunctionPointer = 0;
            long guardXFGTableDispatchFunctionPointer = 0;
            long guardMemcpyFunctionPointer = 0;

            //The tricky thing about load config directory, is that there's no clear delineation between when certain fields were added. People have some rough ideas
            //but about some of the fields, but when you stress test PESpy against system32, you constantly find assemblies that have have one or two fields less than
            //what you thought was included in a given version of the structure; as such, the safest thing to do is to simply assume that you will have no available fields,
            //and only read fields for as long as we are still within the load config directory
            for (var i = 1; reader.Position < end; i++)
            {
                switch (i)
                {
                    #region Default

                    case 1:
                        TimeDateStamp = reader.ReadUInt32();
                        break;

                    case 2:
                        MajorVersion = reader.ReadUInt16();
                        break;

                    case 3:
                        MinorVersion = reader.ReadUInt16();
                        break;

                    case 4:
                        GlobalFlagsClear = reader.ReadInt32(); //Flags
                        break;

                    case 5:
                        GlobalFlagsSet = reader.ReadInt32(); //Flags
                        break;

                    case 6:
                        CriticalSectionDefaultTimeout = reader.ReadInt32();
                        break;

                    case 7:
                        DeCommitFreeBlockThreshold = ReadPointer(reader, is32Bit);
                        break;

                    case 8:
                        DeCommitTotalFreeThreshold = ReadPointer(reader, is32Bit);
                        break;

                    case 9:
                        LockPrefixTable = ReadPointer(reader, is32Bit);
                        break;

                    case 10:
                        MaximumAllocationSize = ReadPointer(reader, is32Bit);
                        break;

                    case 11:
                        VirtualMemoryThreshold = ReadPointer(reader, is32Bit); //Flags
                        break;

                    case 12:
                        if (is32Bit)
                            ProcessHeapFlags = reader.ReadInt32(); //Flags
                        else
                            ProcessAffinityMask = ReadPointer(reader, is32Bit); //Don't think this is flags
                        break;

                    case 13:
                        if (is32Bit)
                            ProcessAffinityMask = ReadPointer(reader, is32Bit); //Don't think this is flags
                        else
                            ProcessHeapFlags = reader.ReadInt32(); //Flags
                        break;

                    case 14:
                        CSDVersion = reader.ReadUInt16();
                        break;

                    case 15:
                        DependentLoadFlags = reader.ReadUInt16(); //Flags
                        break;

                    case 16:
                        EditList = ReadPointer(reader, is32Bit);
                        break;

                    case 17:
                        securityCookie = ReadPointer(reader, is32Bit);
                        break;

                    case 18:
                        sehandlerTable = ReadPointer(reader, is32Bit);
                        break;

                    case 19:
                        SEHandlerCount = ReadPointer(reader, is32Bit);
                        break;

                    #endregion
                    #region Windows SDK 8.1+

                    case 20:
                        guardCFCheckFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 21:
                        guardCFDispatchFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 22:
                        guardCFFunctionTable = ReadPointer(reader, is32Bit);
                        break;

                    case 23:
                        GuardCFFunctionCount = ReadPointer(reader, is32Bit);
                        break;

                    case 24:
                        GuardFlags = (IMAGE_GUARD) reader.ReadUInt32();
                        break;

                    #endregion
                    #region Windows SDK 10.0.10586.0+

                    case 25:
                        CodeIntegrity = new ImageLoadConfigCodeIntegrity(reader);
                        break;

                    case 26:
                        guardAddressTakenIatEntryTable = ReadPointer(reader, is32Bit);
                        break;

                    case 27:
                        GuardAddressTakenIatEntryCount = ReadPointer(reader, is32Bit);
                        break;

                    case 28:
                        guardLongJumpTargetTable = ReadPointer(reader, is32Bit);
                        break;

                    case 29:
                        GuardLongJumpTargetCount = ReadPointer(reader, is32Bit);
                        break;

                    case 30:
                        DynamicValueRelocTable = ReadPointer(reader, is32Bit);
                        break;

                    case 31:
                        CHPEMetadataPointer = ReadPointer(reader, is32Bit);
                        break;

                    #endregion
                    #region Windows SDK 10.0.15063.468+

                    case 32:
                        GuardRFFailureRoutine = ReadPointer(reader, is32Bit);
                        break;

                    case 33:
                        guardRFFailureRoutineFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 34:
                        dynamicValueRelocTableOffset = reader.ReadInt32();
                        break;

                    case 35:
                        DynamicValueRelocTableSection = reader.ReadUInt16();
                        break;

                    case 36:
                        Reserved2 = reader.ReadUInt16();
                        break;

                    case 37:
                        guardRFVerifyStackPointerFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 38:
                        HotPatchTableOffset = reader.ReadInt32();
                        break;

                    case 39:
                        Reserved3 = reader.ReadInt32();
                        break;

                    case 40:
                        enclaveConfigurationPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 41:
                        VolatileMetadataPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 42:
                        guardEHContinuationTable = ReadPointer(reader, is32Bit);
                        break;

                    case 43:
                        GuardEHContinuationCount = ReadPointer(reader, is32Bit);
                        break;

                    case 44:
                        guardXFGCheckFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 45:
                        guardXFGDispatchFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 46:
                        guardXFGTableDispatchFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    case 47:
                        CastGuardOsDeterminedFailureMode = ReadPointer(reader, is32Bit);
                        break;

                    #endregion
                    #region Windows SDK 10.0.22621+

                    case 48:
                        guardMemcpyFunctionPointer = ReadPointer(reader, is32Bit);
                        break;

                    #endregion

                    default:
                        UnknownBytes = reader.ReadBytes(Size - (int) (reader.Position - start));
                        break;
                }
            }

            #endregion
            #region Data

            //Unfortunately I can't move these into functions, because trying to return a struct will create a copy,
            //and we can't use out parameters, as we need to assign the results to properties

            #region LockPrefixTable

            Debug.Assert(LockPrefixTable == 0, $"Don't know how to handle {LockPrefixTable}");

            #endregion
            #region EditList

            Debug.Assert(EditList == 0, $"Don't know how to handle {EditList}");

            #endregion
            #region SecurityCookie

            if (securityCookie != 0)
            {
                var rva = (int) (securityCookie - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    //8 bytes in x32 and x64

                    reader.Seek(offset);

                    var value = reader.ReadUInt64();

                    SecurityCookie = new VA<ulong>(securityCookie, offset, value);
                }
                else
                    SecurityCookie = new VA<ulong>(securityCookie);
            }
            else
                SecurityCookie = default;

            #endregion
            #region SEHandlerTable

            if (sehandlerTable != 0)
            {
                var rva = (int) (sehandlerTable - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    var entries = new long[SEHandlerCount];

                    for (var i = 0; i < SEHandlerCount; i++)
                        entries[i] = reader.ReadInt32();

                    SEHandlerTable = new VA<long[]>(sehandlerTable, offset, entries);
                }
                else
                    SEHandlerTable = new VA<long[]>(sehandlerTable);
            }

            #endregion

            GuardCFCheckFunctionPointer    = GetFunctionPointer(guardCFCheckFunctionPointer, reader, peFile, is32Bit);
            GuardCFDispatchFunctionPointer = GetFunctionPointer(guardCFDispatchFunctionPointer, reader, peFile, is32Bit);

            #region GuardCFFunctionTable

            if (guardCFFunctionTable != 0)
            {
                //GuardCFFunctionTable lists a Virtual Address (which includes the module base).

                var rva = (int) (guardCFFunctionTable - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    GuardCFFunctionTable = new VA<GuardCFFunctionTable>(
                        guardCFFunctionTable,
                        offset,
                        new GuardCFFunctionTable(reader, peFile, GuardFlags, GuardCFFunctionCount)
                    );
                }
                else
                    GuardCFFunctionTable = new VA<GuardCFFunctionTable>(guardCFFunctionTable);
            }

            #endregion
            #region GuardAddressTakenIatEntryTable

            if (guardAddressTakenIatEntryTable != 0)
            {
                var rva = (int) (guardAddressTakenIatEntryTable - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    GuardAddressTakenIatEntryTable = new VA<GuardAddressTakenIatEntryTable>(
                        guardLongJumpTargetTable,
                        offset,
                        new GuardAddressTakenIatEntryTable(reader, GuardFlags, GuardAddressTakenIatEntryCount)
                    );
                }
                else
                    GuardAddressTakenIatEntryTable = new VA<GuardAddressTakenIatEntryTable>(guardAddressTakenIatEntryTable);
            }

            #endregion
            #region GuardLongJumpTargetTable

            if (guardLongJumpTargetTable != 0)
            {
                var rva = (int) (guardLongJumpTargetTable - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    GuardLongJumpTargetTable = new VA<GuardLongJumpTargetTable>(
                        guardLongJumpTargetTable,
                        offset,
                        new GuardLongJumpTargetTable(reader, GuardFlags, GuardLongJumpTargetCount)
                    );
                }
                else
                    GuardLongJumpTargetTable = new VA<GuardLongJumpTargetTable>(guardLongJumpTargetTable);
            }

            #endregion
            #region DynamicValueRelocTable

            //Need to investigate how this relates to DynamicValueRelocTableOffset/DynamicValueRelocTableSection, and if
            //they point towards the same thing
            Debug.Assert(DynamicValueRelocTable == 0, $"Don't know how to handle {DynamicValueRelocTable}");

            #endregion
            #region CHPEMetadataPointer

            Debug.Assert(CHPEMetadataPointer == 0, $"Don't know how to handle {CHPEMetadataPointer}");

            #endregion

            GuardRFFailureRoutineFunctionPointer = GetFunctionPointer(guardRFFailureRoutineFunctionPointer, reader, peFile, is32Bit);

            #region DynamicValueRelocTableOffset

            if (dynamicValueRelocTableOffset != 0)
            {
                if (DynamicValueRelocTableSection != 0)
                {
                    //DynamicValueRelocTableSection lists the 1-based section index. Convert to 0-based
                    var sectionIndex = DynamicValueRelocTableSection - 1;

                    if (sectionIndex < peFile.SectionHeaders.Length)
                    {
                        var section = peFile.SectionHeaders[sectionIndex];

                        var rva = section.VirtualAddress + dynamicValueRelocTableOffset;

                        if (peFile.TryGetOffset(rva, out var offset))
                        {
                            reader.Seek(offset);

                            DynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(
                                dynamicValueRelocTableOffset,
                                offset,
                                new ImageDynamicRelocationTable(reader, peFile)
                            );
                        }
                        else
                            DynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(dynamicValueRelocTableOffset);
                    }
                }
                else
                    DynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(dynamicValueRelocTableOffset);
            }

            #endregion

            GuardRFVerifyStackPointerFunctionPointer = GetFunctionPointer(guardRFVerifyStackPointerFunctionPointer, reader, peFile, is32Bit);

            #region HotPatchTableOffset

            Debug.Assert(HotPatchTableOffset == 0, $"Don't know how to handle {HotPatchTableOffset}");

            #endregion
            #region EnclaveConfigurationPointer

            if (enclaveConfigurationPointer != 0)
            {
                var rva = (int) (enclaveConfigurationPointer - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    EnclaveConfigurationPointer = new VA<ImageEnclaveConfig>(
                        guardEHContinuationTable,
                        offset,
                        new ImageEnclaveConfig(reader, peFile)
                    );
                }
                else
                    EnclaveConfigurationPointer = new VA<ImageEnclaveConfig>(enclaveConfigurationPointer);
            }

            #endregion
            #region VolatileMetadataPointer

            //This points to a structure, however I can't find any information about what this structure is.
            //System Informer seems to think they know what IMAGE_VOLATILE_METADATA is, but I can't find any citation on
            //where they got that name or structure from
            //Debug.Assert(VolatileMetadataPointer == 0, $"Don't know how to handle {VolatileMetadataPointer}");

            #endregion
            #region GuardEHContinuationTable + GuardEHContinuationCount

            //https://learn.microsoft.com/en-us/cpp/build/reference/guard-enable-eh-continuation-metadata?view=msvc-170

            if (guardEHContinuationTable != 0)
            {
                var rva = (int) (guardEHContinuationTable - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    GuardEHContinuationTable = new VA<GuardEHContinuationTable>(
                        guardEHContinuationTable,
                        offset,
                        new GuardEHContinuationTable(reader, GuardFlags, GuardEHContinuationCount)
                    );
                }
                else
                    GuardEHContinuationTable = new VA<GuardEHContinuationTable>(guardEHContinuationTable);
            }

            #endregion

            GuardXFGCheckFunctionPointer         = GetFunctionPointer(guardXFGCheckFunctionPointer,         reader, peFile, is32Bit);
            GuardXFGDispatchFunctionPointer      = GetFunctionPointer(guardXFGDispatchFunctionPointer,      reader, peFile, is32Bit);
            GuardXFGTableDispatchFunctionPointer = GetFunctionPointer(guardXFGTableDispatchFunctionPointer, reader, peFile, is32Bit);
            GuardMemcpyFunctionPointer           = GetFunctionPointer(guardMemcpyFunctionPointer, reader, peFile, is32Bit);

            #endregion
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadPointer(IFileReader reader, bool is32Bit)
        {
            if (is32Bit)
                return reader.ReadUInt32();

            return reader.ReadInt64();
        }

        private static VA<long> GetFunctionPointer(long value, IFileReader reader, PEFile peFile, bool is32Bit)
        {
            //The function pointers are not stored in the load config table; a pointer _to_ the function pointer is stored

            if (value != 0)
            {
                var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetOffset(rva, out var offset))
                {
                    reader.Seek(offset);

                    var fnPtr = ReadPointer(reader, is32Bit);

                    return new VA<long>(value, offset, fnPtr);
                }
                else
                    return new VA<long>(value);
            }
            else
                return default;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("IMAGE_LOAD_CONFIG_DIRECTORY", this, ViewKind.ImageLoadConfigDirectory);

            s.WriteField(nameof(Size), Size);

            for (var i = 1; s.Size < Size; i++)
            {
                switch (i)
                {
                    #region Default

                    case 1:
                        s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
                        break;

                    case 2:
                        s.WriteField(nameof(MajorVersion), MajorVersion);
                        break;

                    case 3:
                        s.WriteField(nameof(MinorVersion), MinorVersion);
                        break;

                    case 4:
                        s.WriteField(nameof(GlobalFlagsClear), GlobalFlagsClear); //Flags
                        break;

                    case 5:
                        s.WriteField(nameof(GlobalFlagsSet), GlobalFlagsSet); //Flags
                        break;

                    case 6:
                        s.WriteField(nameof(CriticalSectionDefaultTimeout), CriticalSectionDefaultTimeout);
                        break;

                    case 7:
                        s.WritePointerField(nameof(DeCommitFreeBlockThreshold), DeCommitFreeBlockThreshold);
                        break;

                    case 8:
                        s.WritePointerField(nameof(DeCommitTotalFreeThreshold), DeCommitTotalFreeThreshold);
                        break;

                    case 9:
                        s.WritePointerField(nameof(LockPrefixTable), LockPrefixTable);
                        break;

                    case 10:
                        s.WritePointerField(nameof(MaximumAllocationSize), MaximumAllocationSize);
                        break;

                    case 11:
                        s.WritePointerField(nameof(VirtualMemoryThreshold), VirtualMemoryThreshold); //Flags
                        break;

                    case 12:
                        if (((PEViewWriter) writer).Is32Bit)
                            s.WriteField(nameof(ProcessHeapFlags), ProcessHeapFlags); //Flags
                        else
                            s.WritePointerField(nameof(ProcessAffinityMask), ProcessAffinityMask); //Don't think this is flags
                        break;

                    case 13:
                        if (((PEViewWriter) writer).Is32Bit)
                            s.WritePointerField(nameof(ProcessAffinityMask), ProcessAffinityMask); //Don't think this is flags
                        else
                            s.WriteField(nameof(ProcessHeapFlags), ProcessHeapFlags); //Flags
                        break;

                    case 14:
                        s.WriteField(nameof(CSDVersion), CSDVersion);
                        break;

                    case 15:
                        s.WriteField(nameof(DependentLoadFlags), DependentLoadFlags); //Flags
                        break;

                    case 16:
                        s.WritePointerField(nameof(EditList), EditList);
                        break;

                    case 17:
                        s.WriteVAPointerField(nameof(SecurityCookie), SecurityCookie, ViewKind.SecurityCookie);
                        break;

                    case 18:
                        s.WriteVAPointerField(nameof(SEHandlerTable), SEHandlerTable, ViewKind.SEHandlerTable);
                        break;

                    case 19:
                        s.WritePointerField(nameof(SEHandlerCount), SEHandlerCount);
                        break;

                    #endregion
                    #region Windows SDK 8.1+

                    case 20:
                        s.WriteVAPointerField(nameof(GuardCFCheckFunctionPointer), GuardCFCheckFunctionPointer, ViewKind.GuardCFCheckFunctionPointer);
                        break;

                    case 21:
                        s.WriteVAPointerField(nameof(GuardCFDispatchFunctionPointer), GuardCFDispatchFunctionPointer, ViewKind.GuardCFDispatchFunctionPointer);
                        break;

                    case 22:
                        s.WriteVAPointerField(nameof(GuardCFFunctionTable), GuardCFFunctionTable);
                        break;

                    case 23:
                        s.WritePointerField(nameof(GuardCFFunctionCount), GuardCFFunctionCount);
                        break;

                    case 24:
                        s.WriteField(nameof(GuardFlags), GuardFlags, sizeof(int));
                        break;

                    #endregion
                    #region Windows SDK 10.0.10586.0+

                    case 25:
                        s.WriteInline(CodeIntegrity);
                        break;

                    case 26:
                        s.WriteVAPointerField(nameof(GuardAddressTakenIatEntryTable), GuardAddressTakenIatEntryTable);
                        break;

                    case 27:
                        s.WritePointerField(nameof(GuardAddressTakenIatEntryCount), GuardAddressTakenIatEntryCount);
                        break;

                    case 28:
                        s.WriteVAPointerField(nameof(GuardLongJumpTargetTable), GuardLongJumpTargetTable);
                        break;

                    case 29:
                        s.WritePointerField(nameof(GuardLongJumpTargetCount), GuardLongJumpTargetCount);
                        break;

                    case 30:
                        s.WritePointerField(nameof(DynamicValueRelocTable), DynamicValueRelocTable);
                        break;

                    case 31:
                        s.WritePointerField(nameof(CHPEMetadataPointer), CHPEMetadataPointer);
                        break;

                    #endregion
                    #region Windows SDK 10.0.15063.468+

                    case 32:
                        s.WritePointerField(nameof(GuardRFFailureRoutine), GuardRFFailureRoutine);
                        break;

                    case 33:
                        s.WriteVAPointerField(nameof(GuardRFFailureRoutineFunctionPointer), GuardRFFailureRoutineFunctionPointer, ViewKind.GuardRFFailureRoutineFunctionPointer);
                        break;

                    case 34:
                        s.WriteField(nameof(DynamicValueRelocTableOffset), DynamicValueRelocTableOffset.ListedOffset);

                        if (DynamicValueRelocTableOffset.IsValid)
                            writer.WriteGlobal((IViewable) DynamicValueRelocTableOffset.Value);
                        break;

                    case 35:
                        s.WriteField(nameof(DynamicValueRelocTableSection), DynamicValueRelocTableSection);
                        break;

                    case 36:
                        s.WriteField(nameof(Reserved2), Reserved2);
                        break;

                    case 37:
                        s.WriteVAPointerField(nameof(GuardRFVerifyStackPointerFunctionPointer), GuardRFVerifyStackPointerFunctionPointer, ViewKind.GuardRFVerifyStackPointerFunctionPointer);
                        break;

                    case 38:
                        s.WriteField(nameof(HotPatchTableOffset), HotPatchTableOffset);
                        break;

                    case 39:
                        s.WriteField(nameof(Reserved3), Reserved3);
                        break;

                    case 40:
                        s.WriteVAPointerField(nameof(EnclaveConfigurationPointer), EnclaveConfigurationPointer);
                        break;

                    case 41:
                        s.WritePointerField(nameof(VolatileMetadataPointer), VolatileMetadataPointer);
                        break;

                    case 42:
                        s.WriteVAPointerField(nameof(GuardEHContinuationTable), GuardEHContinuationTable);
                        break;

                    case 43:
                        s.WritePointerField(nameof(GuardEHContinuationCount), GuardEHContinuationCount);
                        break;

                    case 44:
                        s.WriteVAPointerField(nameof(GuardXFGCheckFunctionPointer), GuardXFGCheckFunctionPointer, ViewKind.GuardXFGCheckFunctionPointer);
                        break;

                    case 45:
                        s.WriteVAPointerField(nameof(GuardXFGDispatchFunctionPointer), GuardXFGDispatchFunctionPointer, ViewKind.GuardXFGDispatchFunctionPointer);
                        break;

                    case 46:
                        s.WriteVAPointerField(nameof(GuardXFGTableDispatchFunctionPointer), GuardXFGTableDispatchFunctionPointer, ViewKind.GuardXFGTableDispatchFunctionPointer);
                        break;

                    case 47:
                        s.WritePointerField(nameof(CastGuardOsDeterminedFailureMode), CastGuardOsDeterminedFailureMode);
                        break;

                    #endregion
                    #region Windows SDK 10.0.22621+

                    case 48:
                        s.WriteVAPointerField(nameof(GuardMemcpyFunctionPointer), GuardMemcpyFunctionPointer, ViewKind.GuardMemcpyFunctionPointer);
                        break;

                    #endregion

                    default:
                        s.WriteField("<UnknownBytes>", UnknownBytes);
                        break;
                }
            }
        }
    }
}
