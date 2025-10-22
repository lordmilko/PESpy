using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy.NE
{
    //There are two types: new_seg and new_seg1. Not sure how to know when each one is used. new_seg1 has an additional field:
    //ns_handle

    public readonly struct ImageOS2Header : IValue, IViewable
    {
        public const ushort IMAGE_OS2_SIGNATURE = 0x454E;    //NE

        private const int MagicOffset = 0;
        private const int VersionNumberOffset = 2;
        private const int RevisionNumberOffset = 3;
        private const int OffsetOfEntryTableOffset = 4;
        private const int NumberOfBytesInEntryTableOffset = 6;
        private const int ChecksumOfWholeFileOffset = 8;
        private const int FlagsOffset = 12;
        private const int AutomaticDataSegmentNumberOffset = 14;
        private const int InitialHeapAllocationOffset = 16;
        private const int InitialStackAllocationOffset = 18;
        private const int InitialCSIPSettingOffset = 20;
        private const int InitialSSSPSettingOffset = 24;
        private const int CountOfFileSegmentsOffset = 28;
        private const int EntriesInModuleReferenceTableOffset = 30;
        private const int SizeOfNonResidentNameTableOffset = 32;
        private const int OffsetOfSegmentTableOffset = 34;
        private const int OffsetOfResourceTableOffset = 36;
        private const int OffsetOfResidentNameTableOffset = 38;
        private const int OffsetOfModuleReferenceTableOffset = 40;
        private const int OffsetOfImportedNamesTableOffset = 42;
        private const int OffsetOfNonResidentNamesTableOffset = 44;
        private const int CountOfMovableEntriesOffset = 48;
        private const int SegmentAlignmentShiftCountOffset = 50;
        private const int CountOfResourceSegmentsOffset = 52;
        private const int TargetOperatingSystemOffset = 54;
        private const int OtherExeFlagsOffset = 55;
        private const int OffsetToReturnThunksOffset = 56;
        private const int OffsetToSegmentRefBytesOffset = 58;
        private const int MinimumCodeSwapAreaSizeOffset = 60;
        private const int ExpectedWindowsVersionNumberOffset = 62;

        /// <summary>
        /// Magic number<para/>
        /// ne_magic
        /// </summary>
        public ushort Magic => chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// Version number (major version number of LINK.EXE)<para/>
        /// ne_ver
        /// </summary>
        public byte VersionNumber => chunk.PeekByte(VersionNumberOffset);

        /// <summary>
        /// Revision number (minor version number of LINK.EXE)<para/>
        /// ne_rev
        /// </summary>
        public byte RevisionNumber => chunk.PeekByte(RevisionNumberOffset);

        /// <summary>
        /// Offset of Entry Table<para/>
        /// ne_enttab
        /// </summary>
        public ushort OffsetOfEntryTable => chunk.PeekUInt16(OffsetOfEntryTableOffset);

        /// <summary>
        /// Number of bytes in Entry Table<para/>
        /// ne_cbenttab
        /// </summary>
        public ushort NumberOfBytesInEntryTable => chunk.PeekUInt16(NumberOfBytesInEntryTableOffset);

        /// <summary>
        /// Checksum of whole file<para/>
        /// ne_crc
        /// </summary>
        public int ChecksumOfWholeFile => chunk.PeekInt32(ChecksumOfWholeFileOffset);

        /// <summary>
        /// Flag word<para/>
        /// ne_flags
        /// </summary>
        public NewExeFlags Flags => (NewExeFlags) chunk.PeekUInt16(FlagsOffset);

        /// <summary>
        /// Automatic data segment number<para/>
        /// ne_autodata
        /// </summary>
        public ushort AutomaticDataSegmentNumber => chunk.PeekUInt16(AutomaticDataSegmentNumberOffset);

        /// <summary>
        /// Initial heap allocation<para/>
        /// ne_heap
        /// </summary>
        public ushort InitialHeapAllocation => chunk.PeekUInt16(InitialHeapAllocationOffset);

        /// <summary>
        /// Initial stack allocation<para/>
        /// ne_stack
        /// </summary>
        public ushort InitialStackAllocation => chunk.PeekUInt16(InitialStackAllocationOffset);

        /// <summary>
        /// Initial CS:IP setting<para/>
        /// ne_csip
        /// </summary>
        public int InitialCSIPSetting => chunk.PeekInt32(InitialCSIPSettingOffset);

        /// <summary>
        /// Initial SS:SP setting<para/>
        /// ne_sssp
        /// </summary>
        public int InitialSSSPSetting => chunk.PeekInt32(InitialSSSPSettingOffset);

        /// <summary>
        /// Count of file segments<para/>
        /// ne_cseg
        /// </summary>
        public ushort CountOfFileSegments => chunk.PeekUInt16(CountOfFileSegmentsOffset);

        /// <summary>
        /// Entries in Module Reference Table<para/>
        /// ne_cmod
        /// </summary>
        public ushort EntriesInModuleReferenceTable => chunk.PeekUInt16(EntriesInModuleReferenceTableOffset);

        /// <summary>
        /// Size of non-resident name table<para/>
        /// ne_cbnrestab
        /// </summary>
        public ushort SizeOfNonResidentNameTable => chunk.PeekUInt16(SizeOfNonResidentNameTableOffset);

        //I think a common pattern that is used is to say that, if the offset of a given table entry is equal to the offset
        //of the entry after it, that entry does not exist

        /// <summary>
        /// Offset of Segment Table<para/>
        /// ne_segtab
        /// </summary>
        public ushort OffsetOfSegmentTable => chunk.PeekUInt16(OffsetOfSegmentTableOffset);

        /// <summary>
        /// Offset of Resource Table<para/>
        /// If this value is equal to <see cref="OffsetOfResidentNameTable"/> (ne_rsrctab == ne_restab) there are no resources.
        /// ne_rsrctab
        /// </summary>
        public ushort OffsetOfResourceTable => chunk.PeekUInt16(OffsetOfResourceTableOffset);

        /// <summary>
        /// Offset of resident name table<para/>
        /// ne_restab
        /// </summary>
        public ushort OffsetOfResidentNameTable => chunk.PeekUInt16(OffsetOfResidentNameTableOffset);

        /// <summary>
        /// Offset of Module Reference Table<para/>
        /// ne_modtab
        /// </summary>
        public ushort OffsetOfModuleReferenceTable => chunk.PeekUInt16(OffsetOfModuleReferenceTableOffset);

        /// <summary>
        /// Offset of Imported Names Table<para/>
        /// ne_imptab
        /// </summary>
        public ushort OffsetOfImportedNamesTable => chunk.PeekUInt16(OffsetOfImportedNamesTableOffset);

        /// <summary>
        /// Offset of Non-resident Names Table<para/>
        /// ne_nrestab
        /// </summary>
        public int OffsetOfNonResidentNamesTable => chunk.PeekInt32(OffsetOfNonResidentNamesTableOffset);

        /// <summary>
        /// Count of movable entries<para/>
        /// ne_cmovent
        /// </summary>
        public ushort CountOfMovableEntries => chunk.PeekUInt16(CountOfMovableEntriesOffset);

        /// <summary>
        /// Segment alignment shift count<para/>
        /// ne_align
        /// </summary>
        public ushort SegmentAlignmentShiftCount => chunk.PeekUInt16(SegmentAlignmentShiftCountOffset);

        /// <summary>
        /// Count of resource segments<para/>
        /// ne_cres
        /// </summary>
        public ushort CountOfResourceSegments => chunk.PeekUInt16(CountOfResourceSegmentsOffset);

        /// <summary>
        /// Target Operating system<para/>
        /// ne_exetyp
        /// </summary>
        public NewOperatingSystem TargetOperatingSystem => (NewOperatingSystem) chunk.PeekByte(TargetOperatingSystemOffset);

        /// <summary>
        /// Other .EXE flags<para/>
        /// ne_flagsothers
        /// </summary>
        public NewOtherExeFlags OtherExeFlags => (NewOtherExeFlags) chunk.PeekByte(OtherExeFlagsOffset);

        /// <summary>
        /// offset to return thunks<para/>
        /// ne_pretthunks
        /// </summary>
        public ushort OffsetToReturnThunks => chunk.PeekUInt16(OffsetToReturnThunksOffset);

        /// <summary>
        /// offset to segment ref. bytes<para/>
        /// ne_psegrefbytes
        /// </summary>
        public ushort OffsetToSegmentRefBytes => chunk.PeekUInt16(OffsetToSegmentRefBytesOffset);

        /// <summary>
        /// Minimum code swap area size<para/>
        /// ne_swaparea
        /// </summary>
        public ushort MinimumCodeSwapAreaSize => chunk.PeekUInt16(MinimumCodeSwapAreaSizeOffset);

        /// <summary>
        /// Expected Windows version number<para/>
        /// ne_expver
        /// </summary>
        public ushort ExpectedWindowsVersionNumber => chunk.PeekUInt16(ExpectedWindowsVersionNumberOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //Magic
            sizeof(byte) + //VersionNumber
            sizeof(byte) + //RevisionNumber
            sizeof(ushort) + //OffsetOfEntryTable
            sizeof(ushort) + //NumberOfBytesInEntryTable
            sizeof(int) + //ChecksumOfWholeFile
            sizeof(ushort) + //Flags
            sizeof(ushort) + //AutomaticDataSegmentNumber
            sizeof(ushort) + //InitialHeapAllocation
            sizeof(ushort) + //InitialStackAllocation
            sizeof(int) + //InitialCSIPSetting
            sizeof(int) + //InitialSSSPSetting
            sizeof(ushort) + //CountOfFileSegments
            sizeof(ushort) + //EntriesInModuleReferenceTable
            sizeof(ushort) + //SizeOfNonResidentNameTable
            sizeof(ushort) + //OffsetOfSegmentTable
            sizeof(ushort) + //OffsetOfResourceTable
            sizeof(ushort) + //OffsetOfResidentNameTable
            sizeof(ushort) + //OffsetOfModuleReferenceTable
            sizeof(ushort) + //OffsetOfImportedNamesTable
            sizeof(int) + //OffsetOfNonResidentNamesTable
            sizeof(ushort) + //CountOfMovableEntries
            sizeof(ushort) + //SegmentAlignmentShiftCount
            sizeof(ushort) + //CountOfResourceSegments
            sizeof(byte) + //TargetOperatingSystem
            sizeof(byte) + //OtherExeFlags
            sizeof(ushort) + //OffsetToReturnThunks
            sizeof(ushort) + //OffsetToSegmentRefBytes
            sizeof(ushort) + //MinimumCodeSwapAreaSize
            sizeof(ushort); //ExpectedWindowsVersionNumber

        private readonly MemoryChunk chunk;

        internal ImageOS2Header(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        //Also called "new_exe"
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_OS2_HEADER, this, ViewKind.ImageOS2Header, StructSize);

        int IViewable.NumChildren() => 30;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("ne_magic", MagicOffset, Magic);
                    break;

                case 1:
                    structWriter.WriteField("ne_ver", VersionNumberOffset, VersionNumber);
                    break;

                case 2:
                    structWriter.WriteField("ne_rev", RevisionNumberOffset, RevisionNumber);
                    break;

                case 3:
                    structWriter.WriteField("ne_enttab", OffsetOfEntryTableOffset, OffsetOfEntryTable);
                    break;

                case 4:
                    structWriter.WriteField("ne_cbenttab", NumberOfBytesInEntryTableOffset, NumberOfBytesInEntryTable);
                    break;

                case 5:
                    structWriter.WriteField("ne_crc", ChecksumOfWholeFileOffset, ChecksumOfWholeFile);
                    break;

                case 6:
                    structWriter.WriteField("ne_flags", FlagsOffset, Flags, sizeof(short));
                    break;

                case 7:
                    structWriter.WriteField("ne_autodata", AutomaticDataSegmentNumberOffset, AutomaticDataSegmentNumber);
                    break;

                case 8:
                    structWriter.WriteField("ne_heap", InitialHeapAllocationOffset, InitialHeapAllocation);
                    break;

                case 9:
                    structWriter.WriteField("ne_stack", InitialStackAllocationOffset, InitialStackAllocation);
                    break;

                case 10:
                    structWriter.WriteField("ne_csip", InitialCSIPSettingOffset, InitialCSIPSetting);
                    break;

                case 11:
                    structWriter.WriteField("ne_sssp", InitialSSSPSettingOffset, InitialSSSPSetting);
                    break;

                case 12:
                    structWriter.WriteField("ne_cseg", CountOfFileSegmentsOffset, CountOfFileSegments);
                    break;

                case 13:
                    structWriter.WriteField("ne_cmod", EntriesInModuleReferenceTableOffset, EntriesInModuleReferenceTable);
                    break;

                case 14:
                    structWriter.WriteField("ne_cbnrestab", SizeOfNonResidentNameTableOffset, SizeOfNonResidentNameTable);
                    break;

                case 15:
                    structWriter.WriteField("ne_segtab", OffsetOfSegmentTableOffset, OffsetOfSegmentTable);
                    break;

                case 16:
                    structWriter.WriteField("ne_rsrctab", OffsetOfResourceTableOffset, OffsetOfResourceTable);
                    break;

                case 17:
                    structWriter.WriteField("ne_restab", OffsetOfResidentNameTableOffset, OffsetOfResidentNameTable);
                    break;

                case 18:
                    structWriter.WriteField("ne_modtab", OffsetOfModuleReferenceTableOffset, OffsetOfModuleReferenceTable);
                    break;

                case 19:
                    structWriter.WriteField("ne_imptab", OffsetOfImportedNamesTableOffset, OffsetOfImportedNamesTable);
                    break;

                case 20:
                    structWriter.WriteField("ne_nrestab", OffsetOfNonResidentNamesTableOffset, OffsetOfNonResidentNamesTable);
                    break;

                case 21:
                    structWriter.WriteField("ne_cmovent", CountOfMovableEntriesOffset, CountOfMovableEntries);
                    break;

                case 22:
                    structWriter.WriteField("ne_align", SegmentAlignmentShiftCountOffset, SegmentAlignmentShiftCount);
                    break;

                case 23:
                    structWriter.WriteField("ne_cres", CountOfResourceSegmentsOffset, CountOfResourceSegments);
                    break;

                case 24:
                    structWriter.WriteField("ne_exetyp", TargetOperatingSystemOffset, TargetOperatingSystem, sizeof(byte));
                    break;

                case 25:
                    structWriter.WriteField("ne_flagsothers", OtherExeFlagsOffset, OtherExeFlags, sizeof(byte));
                    break;

                case 26:
                    structWriter.WriteField("ne_pretthunks", OffsetToReturnThunksOffset, OffsetToReturnThunks);
                    break;

                case 27:
                    structWriter.WriteField("ne_psegrefbytes", OffsetToSegmentRefBytesOffset, OffsetToSegmentRefBytes);
                    break;

                case 28:
                    structWriter.WriteField("ne_swaparea", MinimumCodeSwapAreaSizeOffset, MinimumCodeSwapAreaSize);
                    break;

                case 29:
                    structWriter.WriteField("ne_expver", ExpectedWindowsVersionNumberOffset, ExpectedWindowsVersionNumber);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
