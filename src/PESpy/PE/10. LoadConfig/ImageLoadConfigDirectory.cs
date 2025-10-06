using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

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

        private int LockPrefixTableOffset => 24 + (2 * chunk.PointerSize);
        private int SecurityCookieOffset => 32 + (7 * chunk.PointerSize);
        private int SEHandlerTableOffset => 32 + (8 * chunk.PointerSize);
        private int GuardCFCheckFunctionPointerOffset => 32 + (10 * chunk.PointerSize);
        private int GuardCFDispatchFunctionPointerOffset => 32 + (11 * chunk.PointerSize);
        private int GuardCFFunctionTableOffset => 32 + (12 * chunk.PointerSize);
        private int GuardAddressTakenIatEntryTableOffset => 48 + (14 * chunk.PointerSize);
        private int GuardLongJumpTargetTableOffset => 48 + (16 * chunk.PointerSize);
        private int GuardRFFailureRoutineFunctionPointerOffset => 48 + (21 * chunk.PointerSize);
        private int DynamicValueRelocTableOffsetOffset => 48 + (22 * chunk.PointerSize);
        private int GuardRFVerifyStackPointerFunctionPointerOffset => 56 + (22 * chunk.PointerSize);
        private int EnclaveConfigurationPointerOffset => 64 + (23 * chunk.PointerSize);
        private int GuardEHContinuationTableOffset => 64 + (25 * chunk.PointerSize);
        private int GuardXFGCheckFunctionPointerOffset => 64 + (27 * chunk.PointerSize);
        private int GuardXFGDispatchFunctionPointerOffset => 64 + (28 * chunk.PointerSize);
        private int GuardXFGTableDispatchFunctionPointerOffset => 64 + (29 * chunk.PointerSize);
        private int GuardMemcpyFunctionPointerOffset => 64 + (31 * chunk.PointerSize);

        /// <summary>
        /// The size of the structure. For Windows XP, the size must be specified as 64 for x86 images.
        /// </summary>
        public int Size => chunk.PeekInt32(0);

        #region Default

        /// <summary>
        /// Date and time stamp value. The value is represented in the number of seconds that have elapsed since midnight
        /// (00:00:00), January 1, 1970, Universal Coordinated Time, according to the system clock. The time stamp can be
        /// printed by using the C runtime (CRT) time function.
        /// </summary>
        public Timestamp TimeDateStamp => chunk.TryPeekUInt32(4, Size);

        /// <summary>
        /// Major version number.
        /// </summary>
        public ushort MajorVersion => chunk.TryPeekUInt16(8, Size);

        /// <summary>
        /// Minor version number.
        /// </summary>
        public ushort MinorVersion => chunk.TryPeekUInt16(10, Size);

        /// <summary>
        /// The global loader flags to clear for this process as the loader starts the process.
        /// </summary>
        public int GlobalFlagsClear => chunk.TryPeekInt32(12, Size);

        /// <summary>
        /// The global loader flags to set for this process as the loader starts the process.
        /// </summary>
        public int GlobalFlagsSet => chunk.TryPeekInt32(16, Size);

        /// <summary>
        /// The default timeout value to use for this process's critical sections that are abandoned.
        /// </summary>
        public int CriticalSectionDefaultTimeout => chunk.TryPeekInt32(20, Size);

        /// <summary>
        /// Memory that must be freed before it is returned to the system, in bytes.
        /// </summary>
        public long DeCommitFreeBlockThreshold => chunk.TryPeekPointer(24, Size);

        /// <summary>
        /// Total amount of free memory, in bytes.
        /// </summary>
        public long DeCommitTotalFreeThreshold => chunk.TryPeekPointer(24 + chunk.PointerSize, Size);

        /// <summary>
        /// [x86 only] The VA of a list of addresses where the LOCK prefix is used so that they can be replaced with NOP
        /// on single processor machines.
        /// </summary>
        private VA<ulong[]> lockPrefixTable;

        public VA<ulong[]> LockPrefixTable
        {
            get
            {
                if (lockPrefixTable.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(LockPrefixTableOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            using var entries = new PooledList<ulong>();

                            var pointerSize = chunk.PointerSize;

                            var read = 0;

                            while (true)
                            {
                                var entry = valueChunk.PeekPointer(read);
                                read += pointerSize;

                                entries.Add(entry);

                                if (entry == 0)
                                    break;
                            }

                            lockPrefixTable = new VA<ulong[]>(value, valueChunk.AbsoluteOffset, entries.ToArray());
                        }
                        else
                            lockPrefixTable = new VA<ulong[]>(value);
                    }
                }

                return lockPrefixTable;

                
            }
        }

        /// <summary>
        /// Maximum allocation size, in bytes.
        /// </summary>
        public long MaximumAllocationSize => chunk.TryPeekPointer(24 + (3 * chunk.PointerSize), Size);

        /// <summary>
        /// Maximum virtual memory size, in bytes.
        /// </summary>
        public long VirtualMemoryThreshold => chunk.TryPeekPointer(24 + (4 * chunk.PointerSize), Size);

        /// <summary>
        /// Setting this field to a non-zero value is equivalent to calling SetProcessAffinityMask with this value
        /// during process startup (.exe only)
        /// </summary>
        public long ProcessAffinityMask
        {
            get
            {
                if (chunk.Is32Bit)
                {
                    //ProcessHeapFlags, ProcessAffinityMask
                    //So we need to skip over the ProcessHeapFlags (which comes first) to get the ProcessAffinityMask
                    return chunk.TryPeekPointer(28 + (5 * chunk.PointerSize), Size);
                }
                else
                    return chunk.TryPeekPointer(24 + (5 * chunk.PointerSize), Size); //ProcessAffinityMask, ProcessHeapFlags
            }
        }

        /// <summary>
        /// Process heap flags that correspond to the first argument of the HeapCreate function. These flags apply to the
        /// process heap that is created during process startup.
        /// </summary>
        public int ProcessHeapFlags
        {
            get
            {
                if (chunk.Is32Bit)
                {
                    //ProcessHeapFlags, ProcessAffinityMask
                    //So we need to read the value prior to the ProcessAffinityMask
                    return chunk.TryPeekInt32(24 + (5 * chunk.PointerSize), Size);
                }
                else
                    return chunk.TryPeekInt32(24 + (6 * chunk.PointerSize), Size); //ProcessAffinityMask, ProcessHeapFlags
            }
        }

        /// <summary>
        /// The service pack version identifier.
        /// </summary>
        public ushort CSDVersion => chunk.TryPeekUInt16(28 + (6 * chunk.PointerSize), Size);

        /// <summary>
        /// The default load flags used when the operating system resolves the statically linked imports of a module.
        /// </summary>
        public ushort DependentLoadFlags => chunk.TryPeekUInt16(30 + (6 * chunk.PointerSize), Size);

        /// <summary>
        /// Reserved for use by the system.
        /// </summary>
        public long EditList => chunk.TryPeekPointer(32 + (6 * chunk.PointerSize), Size);

        /// <summary>
        /// A pointer to a cookie that is used by Visual C++ or GS implementation.
        /// </summary>
        private VA<ulong> securityCookie;

        public VA<ulong> SecurityCookie
        {
            get
            {
                if (securityCookie.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(SecurityCookieOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            //I previously remarked that this is 8 bytes in x86 and x64, but in stress testing this caused conflicts with the value that comes after it,
                            //so we're back to treating it like a pointer

                            var cookie = valueChunk.PeekPointer(0);

                            securityCookie = new VA<ulong>(value, valueChunk.AbsoluteOffset, cookie);
                        }
                        else
                            securityCookie = new VA<ulong>(value);
                    }
                }

                return securityCookie;
            }
        }

        /// <summary>
        /// [x86 only] The VA of the sorted table of RVAs of each valid, unique SE handler in the image.
        /// </summary>
        private VA<int[]> seHandlerTable;

        public VA<int[]> SEHandlerTable
        {
            get
            {
                if (seHandlerTable.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(SEHandlerTableOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            var entries = new int[SEHandlerCount];

                            for (var i = 0; i < SEHandlerCount; i++)
                                entries[i] = valueChunk.PeekInt32(i * 4);

                            seHandlerTable = new VA<int[]>(value, valueChunk.AbsoluteOffset, entries);
                        }
                        else
                            seHandlerTable = new VA<int[]>(value);
                    }
                }

                return seHandlerTable;
            }
        }

        /// <summary>
        /// [x86 only] The count of unique handlers in the table.
        /// </summary>
        public long SEHandlerCount => chunk.TryPeekPointer(32 + (9 * chunk.PointerSize), Size);

        #endregion
        #region Windows SDK 8.1+

        /// <summary>
        /// The VA where Control Flow Guard check-function pointer is stored.
        /// </summary>
        private VA<long> guardCFCheckFunctionPointer;

        public VA<long> GuardCFCheckFunctionPointer =>
            GetFunctionPointer(ref guardCFCheckFunctionPointer, chunk.TryPeekPointer(GuardCFCheckFunctionPointerOffset, Size));

        /// <summary>
        /// The VA where Control Flow Guard dispatch-function pointer is stored.
        /// </summary>
        private VA<long> guardCFDispatchFunctionPointer;

        public VA<long> GuardCFDispatchFunctionPointer =>
            GetFunctionPointer(ref guardCFDispatchFunctionPointer, chunk.TryPeekPointer(GuardCFDispatchFunctionPointerOffset, Size));

        /// <summary>
        /// The VA of the sorted table of RVAs of each Control Flow Guard function in the image.
        /// </summary>
        private VA<GuardCFFunctionTable> guardCFFunctionTable;

        public VA<GuardCFFunctionTable> GuardCFFunctionTable
        {
            get
            {
                if (guardCFFunctionTable.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(GuardCFFunctionTableOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        //GuardCFFunctionTable lists a Virtual Address (which includes the module base).

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            guardCFFunctionTable = new VA<GuardCFFunctionTable>(
                                value,
                                valueChunk.AbsoluteOffset,
                                new GuardCFFunctionTable(valueChunk, GuardFlags, GuardCFFunctionCount)
                            );
                        }
                        else
                            guardCFFunctionTable = new VA<GuardCFFunctionTable>(value);
                    }
                }

                return guardCFFunctionTable;
            }
        }

        /// <summary>
        /// The count of unique RVAs in the above table.
        /// </summary>
        public long GuardCFFunctionCount => chunk.TryPeekPointer(32 + (13 * chunk.PointerSize), Size);

        /// <summary>
        /// Control Flow Guard related flags.
        /// </summary>
        public IMAGE_GUARD GuardFlags => (IMAGE_GUARD) chunk.TryPeekUInt32(32 + (14 * chunk.PointerSize), Size);

        #endregion
        #region Windows SDK 10.0.10586.0+

        /// <summary>
        /// Code integrity information.
        /// </summary>
        public ImageLoadConfigCodeIntegrity CodeIntegrity
        {
            get
            {
                var offset = 36 + (14 * chunk.PointerSize);

                if (offset < Size)
                    return new ImageLoadConfigCodeIntegrity(chunk.Slice(offset));

                return default;
            }
        }

        /// <summary>
        /// The VA where Control Flow Guard address taken IAT table is stored.
        /// </summary>
        private VA<GuardAddressTakenIatEntryTable> guardAddressTakenIatEntryTable;

        public VA<GuardAddressTakenIatEntryTable> GuardAddressTakenIatEntryTable
        {
            get
            {
                if (guardAddressTakenIatEntryTable.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(GuardAddressTakenIatEntryTableOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            guardAddressTakenIatEntryTable = new VA<GuardAddressTakenIatEntryTable>(
                                value,
                                valueChunk.AbsoluteOffset,
                                new GuardAddressTakenIatEntryTable(valueChunk, GuardFlags, GuardAddressTakenIatEntryCount)
                            );
                        }
                    }
                }

                return guardAddressTakenIatEntryTable;
            }
        }

        /// <summary>
        /// The count of unique RVAs in the above table.
        /// </summary>
        public long GuardAddressTakenIatEntryCount => chunk.TryPeekPointer(48 + (15 * chunk.PointerSize), Size);

        /// <summary>
        /// The VA where Control Flow Guard long jump target table is stored.
        /// </summary>
        private VA<GuardLongJumpTargetTable> guardLongJumpTargetTable;

        public VA<GuardLongJumpTargetTable> GuardLongJumpTargetTable
        {
            get
            {
                if (guardLongJumpTargetTable.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(GuardLongJumpTargetTableOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            guardLongJumpTargetTable = new VA<GuardLongJumpTargetTable>(
                                value,
                                valueChunk.AbsoluteOffset,
                                new GuardLongJumpTargetTable(valueChunk, GuardFlags, GuardLongJumpTargetCount)
                            );
                        }
                        else
                            guardLongJumpTargetTable = new VA<GuardLongJumpTargetTable>(value);
                    }
                }

                return guardLongJumpTargetTable;
            }
        }

        /// <summary>
        /// The count of unique RVAs in the above table.
        /// </summary>
        public long GuardLongJumpTargetCount => chunk.TryPeekPointer(48 + (17 * chunk.PointerSize), Size);

        //Need to investigate how this relates to DynamicValueRelocTableOffset/DynamicValueRelocTableSection, and if
        //they point towards the same thing
        public long DynamicValueRelocTable => chunk.TryPeekPointer(48 + (18 * chunk.PointerSize), Size);

        //I think this is IMAGE_CHPE_RANGE_ENTRY?
        public long CHPEMetadataPointer => chunk.TryPeekPointer(48 + (19 * chunk.PointerSize), Size);

        #endregion
        #region Windows SDK 10.0.15063.468+

        public long GuardRFFailureRoutine => chunk.TryPeekPointer(48 + (20 * chunk.PointerSize), Size);

        private VA<long> guardRFFailureRoutineFunctionPointer;

        public VA<long> GuardRFFailureRoutineFunctionPointer =>
            GetFunctionPointer(ref guardRFFailureRoutineFunctionPointer, chunk.TryPeekPointer(GuardRFFailureRoutineFunctionPointerOffset, Size));

        private RVA<ImageDynamicRelocationTable> dynamicValueRelocTableOffset;

        public RVA<ImageDynamicRelocationTable> DynamicValueRelocTableOffset
        {
            get
            {
                if (dynamicValueRelocTableOffset.ListedOffset == 0)
                {
                    var value = chunk.TryPeekInt32(DynamicValueRelocTableOffsetOffset, Size);

                    if (value != 0)
                    {
                        var sectionNumber = DynamicValueRelocTableSection;

                        if (sectionNumber != 0)
                        {
                            //DynamicValueRelocTableSection lists the 1-based section index. Convert to 0-based
                            var sectionIndex = sectionNumber - 1;

                            var peFile = chunk.PEFile();

                            if (sectionIndex < peFile.SectionHeaders.Length)
                            {
                                ref var section = ref peFile.SectionHeaders[sectionIndex];

                                var rva = section.VirtualAddress + value;

                                if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                                {
                                    dynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(
                                        value,
                                        valueChunk.AbsoluteOffset,
                                        new ImageDynamicRelocationTable(valueChunk)
                                    );
                                }
                                else
                                    dynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(value);
                            }
                            else
                                dynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(value);
                        }
                        else
                            dynamicValueRelocTableOffset = new RVA<ImageDynamicRelocationTable>(value);
                    }
                }

                return dynamicValueRelocTableOffset;
            }
        }

        public ushort DynamicValueRelocTableSection => chunk.TryPeekUInt16(52 + (22 * chunk.PointerSize), Size);

        public ushort Reserved2 => chunk.TryPeekUInt16(54 + (22 * chunk.PointerSize), Size);

        private VA<long> guardRFVerifyStackPointerFunctionPointer;

        public VA<long> GuardRFVerifyStackPointerFunctionPointer =>
            GetFunctionPointer(ref guardRFVerifyStackPointerFunctionPointer, chunk.TryPeekPointer(GuardRFVerifyStackPointerFunctionPointerOffset, Size));

        public int HotPatchTableOffset => chunk.TryPeekInt32(56 + (23 * chunk.PointerSize), Size);

        public int Reserved3 => chunk.TryPeekInt32(60 + (23 * chunk.PointerSize), Size);

        private VA<ImageEnclaveConfig> enclaveConfigurationPointer;

        public VA<ImageEnclaveConfig> EnclaveConfigurationPointer
        {
            get
            {
                if (enclaveConfigurationPointer.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(EnclaveConfigurationPointerOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            enclaveConfigurationPointer = new VA<ImageEnclaveConfig>(
                                value,
                                valueChunk.AbsoluteOffset,
                                new ImageEnclaveConfig(valueChunk)
                            );
                        }
                        else
                            enclaveConfigurationPointer = new VA<ImageEnclaveConfig>(value);
                    }
                }

                return enclaveConfigurationPointer;
            }
        }

        //Each successive property from here may or not be present; there is no clear delineation between when each item was added

        //This points to a structure, however I can't find any information about what this structure is.
        //System Informer seems to think they know what IMAGE_VOLATILE_METADATA is, but I can't find any citation on
        //where they got that name or structure from
        public long VolatileMetadataPointer => chunk.TryPeekPointer(64 + (24 * chunk.PointerSize), Size);

        private VA<GuardEHContinuationTable> guardEHContinuationTable;

        public VA<GuardEHContinuationTable> GuardEHContinuationTable
        {
            get
            {
                ////https://learn.microsoft.com/en-us/cpp/build/reference/guard-enable-eh-continuation-metadata?view=msvc-170
                if (guardEHContinuationTable.ListedAddress == 0)
                {
                    var value = chunk.TryPeekPointer(GuardEHContinuationTableOffset, Size);

                    if (value != 0)
                    {
                        var peFile = chunk.PEFile();

                        var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                        if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                        {
                            guardEHContinuationTable = new VA<GuardEHContinuationTable>(
                                value,
                                valueChunk.AbsoluteOffset,
                                new GuardEHContinuationTable(valueChunk, GuardFlags, GuardEHContinuationCount)
                            );
                        }
                        else
                            guardEHContinuationTable = new VA<GuardEHContinuationTable>(value);
                    }
                }

                return guardEHContinuationTable;
            }
        }

        public long GuardEHContinuationCount => chunk.TryPeekPointer(64 + (26 * chunk.PointerSize), Size);

        private VA<long> guardXFGCheckFunctionPointer;

        public VA<long> GuardXFGCheckFunctionPointer =>
            GetFunctionPointer(ref guardXFGCheckFunctionPointer, chunk.TryPeekPointer(GuardXFGCheckFunctionPointerOffset, Size));

        private VA<long> guardXFGDispatchFunctionPointer;

        public VA<long> GuardXFGDispatchFunctionPointer =>
            GetFunctionPointer(ref guardXFGDispatchFunctionPointer, chunk.TryPeekPointer(GuardXFGDispatchFunctionPointerOffset, Size));

        private VA<long> guardXFGTableDispatchFunctionPointer;

        public VA<long> GuardXFGTableDispatchFunctionPointer =>
            GetFunctionPointer(ref guardXFGTableDispatchFunctionPointer, chunk.TryPeekPointer(GuardXFGTableDispatchFunctionPointerOffset, Size));

        public long CastGuardOsDeterminedFailureMode => chunk.TryPeekPointer(64 + (30 * chunk.PointerSize), Size);

        #endregion
        #region Windows SDK 10.0.22621+

        private VA<long> guardMemcpyFunctionPointer;

        public VA<long> GuardMemcpyFunctionPointer =>
            GetFunctionPointer(ref guardMemcpyFunctionPointer, chunk.TryPeekPointer(GuardMemcpyFunctionPointerOffset, Size));

        #endregion

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageLoadConfigDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        private VA<long> GetFunctionPointer(ref VA<long> field, long value)
        {
            if (field.ListedAddress != 0)
                return field;

            if (value == 0)
                return default;

            var peFile = chunk.PEFile();

            var rva = (int) (value - peFile.OptionalHeader.ImageBase);

            if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
            {
                var fnPtr = (long) valueChunk.PeekPointer(0);

                return new VA<long>(value, valueChunk.AbsoluteOffset, fnPtr);
            }
            else
            {
                field = new VA<long>(value);
            }

            return field;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteVAPointerField(LockPrefixTable, ViewKind.LockPrefixTable, fieldOffset: LockPrefixTableOffset); //9

            writer.WriteVAPointerField(SecurityCookie, ViewKind.SecurityCookie, fieldOffset: SecurityCookieOffset); //17
            writer.WriteVAPointerField(SEHandlerTable, ViewKind.SEHandlerTable, fieldOffset: SEHandlerTableOffset); //18

            writer.WriteVAPointerField(GuardCFCheckFunctionPointer, ViewKind.GuardCFCheckFunctionPointer, fieldOffset: GuardCFCheckFunctionPointerOffset); //20
            writer.WriteVAPointerField(GuardCFDispatchFunctionPointer, ViewKind.GuardCFDispatchFunctionPointer, fieldOffset: GuardCFDispatchFunctionPointerOffset); //21

            writer.WriteVAPointerField(GuardCFFunctionTable, fieldOffset: GuardCFFunctionTableOffset); //22
            writer.WriteVAPointerField(GuardAddressTakenIatEntryTable, fieldOffset: GuardAddressTakenIatEntryTableOffset); //26
            writer.WriteVAPointerField(GuardLongJumpTargetTable, fieldOffset: GuardLongJumpTargetTableOffset); //28

            writer.WriteVAPointerField(GuardRFFailureRoutineFunctionPointer, ViewKind.GuardRFFailureRoutineFunctionPointer, fieldOffset: GuardRFFailureRoutineFunctionPointerOffset); //33

            if (DynamicValueRelocTableOffset.IsValid) //34
                writer.WriteGlobal((IViewable) DynamicValueRelocTableOffset.Value);

            writer.WriteVAPointerField(GuardRFVerifyStackPointerFunctionPointer, ViewKind.GuardRFVerifyStackPointerFunctionPointer, fieldOffset: GuardRFVerifyStackPointerFunctionPointerOffset); //37

            writer.WriteVAPointerField(EnclaveConfigurationPointer, fieldOffset: EnclaveConfigurationPointerOffset); //40
            writer.WriteVAPointerField(GuardEHContinuationTable, fieldOffset: GuardEHContinuationTableOffset); //42

            writer.WriteVAPointerField(GuardXFGCheckFunctionPointer, ViewKind.GuardXFGCheckFunctionPointer, fieldOffset: GuardXFGCheckFunctionPointerOffset); //44
            writer.WriteVAPointerField(GuardXFGDispatchFunctionPointer, ViewKind.GuardXFGDispatchFunctionPointer, fieldOffset: GuardXFGDispatchFunctionPointerOffset); //45
            writer.WriteVAPointerField(GuardXFGTableDispatchFunctionPointer, ViewKind.GuardXFGTableDispatchFunctionPointer, fieldOffset: GuardXFGTableDispatchFunctionPointerOffset); //46
            writer.WriteVAPointerField(GuardMemcpyFunctionPointer, ViewKind.GuardMemcpyFunctionPointer, fieldOffset: GuardMemcpyFunctionPointerOffset); //48
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_LOAD_CONFIG_DIRECTORY, this, ViewKind.ImageLoadConfigDirectory, Size);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Size), Size);

            for (var i = 1; s.Size < Size; i++)
            {
                switch (i) //We're switching on i, which is not related to the number of bytes written
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
                        s.WriteVAPointerField(nameof(LockPrefixTable), LockPrefixTable, ViewKind.LockPrefixTable);
                        break;

                    case 10:
                        s.WritePointerField(nameof(MaximumAllocationSize), MaximumAllocationSize);
                        break;

                    case 11:
                        s.WritePointerField(nameof(VirtualMemoryThreshold), VirtualMemoryThreshold); //Flags
                        break;

                    case 12:
                        if (((PEViewWriter) viewWriter).Is32Bit)
                            s.WriteField(nameof(ProcessHeapFlags), ProcessHeapFlags); //Flags
                        else
                            s.WritePointerField(nameof(ProcessAffinityMask), ProcessAffinityMask); //Don't think this is flags
                        break;

                    case 13:
                        if (((PEViewWriter) viewWriter).Is32Bit)
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
                }
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
