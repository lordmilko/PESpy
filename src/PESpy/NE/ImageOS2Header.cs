using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy.NE
{
    //There are two types: new_seg and new_seg1. Not sure how to know when each one is used. new_seg1 has an additional field:
    //ns_handle

    /// <summary>
    /// Represents the <see cref="IMAGE_OS2_HEADER"/> structure that identifies a file using the New Executable (NE) format.
    /// </summary>
    [Source(SourceKind.winnt_h | SourceKind.newexe_h)] //Also known as new_exe in newexe.h
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
        /// Magic number
        /// </summary>
        public ushort ne_magic => chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// Version number (major version number of LINK.EXE)
        /// </summary>
        public byte ne_ver => chunk.PeekByte(VersionNumberOffset);

        /// <summary>
        /// Revision number (minor version number of LINK.EXE)
        /// </summary>
        public byte ne_rev => chunk.PeekByte(RevisionNumberOffset);

        /// <summary>
        /// Offset of Entry Table
        /// </summary>
        public ushort ne_enttab => chunk.PeekUInt16(OffsetOfEntryTableOffset);

        /// <summary>
        /// Number of bytes in Entry Table
        /// </summary>
        public ushort ne_cbenttab => chunk.PeekUInt16(NumberOfBytesInEntryTableOffset);

        /// <summary>
        /// Checksum of whole file
        /// </summary>
        public int ne_crc => chunk.PeekInt32(ChecksumOfWholeFileOffset);

        /// <summary>
        /// Flag word
        /// </summary>
        public NewExeFlags ne_flags => (NewExeFlags) chunk.PeekUInt16(FlagsOffset);

        /// <summary>
        /// Automatic data segment number
        /// </summary>
        public ushort ne_autodata => chunk.PeekUInt16(AutomaticDataSegmentNumberOffset);

        /// <summary>
        /// Initial heap allocation
        /// </summary>
        public ushort ne_heap => chunk.PeekUInt16(InitialHeapAllocationOffset);

        /// <summary>
        /// Initial stack allocation
        /// </summary>
        public ushort ne_stack => chunk.PeekUInt16(InitialStackAllocationOffset);

        /// <summary>
        /// Initial CS:IP setting
        /// </summary>
        public int ne_csip => chunk.PeekInt32(InitialCSIPSettingOffset);

        /// <summary>
        /// Initial SS:SP setting
        /// </summary>
        public int ne_sssp => chunk.PeekInt32(InitialSSSPSettingOffset);

        /// <summary>
        /// Count of file segments
        /// </summary>
        public ushort ne_cseg => chunk.PeekUInt16(CountOfFileSegmentsOffset);

        /// <summary>
        /// Entries in Module Reference Table
        /// </summary>
        public ushort ne_cmod => chunk.PeekUInt16(EntriesInModuleReferenceTableOffset);

        /// <summary>
        /// Size of non-resident name table
        /// </summary>
        public ushort ne_cbnrestab => chunk.PeekUInt16(SizeOfNonResidentNameTableOffset);

        //I think a common pattern that is used is to say that, if the offset of a given table entry is equal to the offset
        //of the entry after it, that entry does not exist

        /// <summary>
        /// Offset of Segment Table
        /// </summary>
        public ushort ne_segtab => chunk.PeekUInt16(OffsetOfSegmentTableOffset);

        /// <summary>
        /// Offset of Resource Table<para/>
        /// If this value is equal to <see cref="ne_restab"/> there are no resources.
        /// ne_rsrctab
        /// </summary>
        public ushort ne_rsrctab => chunk.PeekUInt16(OffsetOfResourceTableOffset);

        /// <summary>
        /// Offset of resident name table
        /// </summary>
        public ushort ne_restab => chunk.PeekUInt16(OffsetOfResidentNameTableOffset);

        /// <summary>
        /// Offset of Module Reference Table
        /// </summary>
        public ushort ne_modtab => chunk.PeekUInt16(OffsetOfModuleReferenceTableOffset);

        /// <summary>
        /// Offset of Imported Names Table
        /// </summary>
        public ushort ne_imptab => chunk.PeekUInt16(OffsetOfImportedNamesTableOffset);

        /// <summary>
        /// Offset of Non-resident Names Table
        /// </summary>
        public int ne_nrestab => chunk.PeekInt32(OffsetOfNonResidentNamesTableOffset);

        /// <summary>
        /// Count of movable entries
        /// </summary>
        public ushort ne_cmovent => chunk.PeekUInt16(CountOfMovableEntriesOffset);

        /// <summary>
        /// Segment alignment shift count
        /// </summary>
        public ushort ne_align => chunk.PeekUInt16(SegmentAlignmentShiftCountOffset);

        /// <summary>
        /// Count of resource segments
        /// </summary>
        public ushort ne_cres => chunk.PeekUInt16(CountOfResourceSegmentsOffset);

        /// <summary>
        /// Target Operating system
        /// </summary>
        public NewOperatingSystem ne_exetyp => (NewOperatingSystem) chunk.PeekByte(TargetOperatingSystemOffset);

        /// <summary>
        /// Other .EXE flags
        /// </summary>
        public NewOtherExeFlags ne_flagsothers => (NewOtherExeFlags) chunk.PeekByte(OtherExeFlagsOffset);

        /// <summary>
        /// offset to return thunks
        /// </summary>
        public ushort ne_pretthunks => chunk.PeekUInt16(OffsetToReturnThunksOffset);

        /// <summary>
        /// offset to segment ref. bytes
        /// </summary>
        public ushort ne_psegrefbytes => chunk.PeekUInt16(OffsetToSegmentRefBytesOffset);

        /// <summary>
        /// Minimum code swap area size
        /// </summary>
        public ushort ne_swaparea => chunk.PeekUInt16(MinimumCodeSwapAreaSizeOffset);

        /// <summary>
        /// Expected Windows version number
        /// </summary>
        public ushort ne_expver => chunk.PeekUInt16(ExpectedWindowsVersionNumberOffset);

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
            writer.NewStruct(this, ViewKind.ImageOS2Header, StructSize);

        int IViewable.NumChildren() => 30;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ne_magic), MagicOffset, ne_magic);
                    break;

                case 1:
                    structWriter.WriteField(nameof(ne_ver), VersionNumberOffset, ne_ver);
                    break;

                case 2:
                    structWriter.WriteField(nameof(ne_rev), RevisionNumberOffset, ne_rev);
                    break;

                case 3:
                    structWriter.WriteField(nameof(ne_enttab), OffsetOfEntryTableOffset, ne_enttab);
                    break;

                case 4:
                    structWriter.WriteField(nameof(ne_cbenttab), NumberOfBytesInEntryTableOffset, ne_cbenttab);
                    break;

                case 5:
                    structWriter.WriteField(nameof(ne_crc), ChecksumOfWholeFileOffset, ne_crc);
                    break;

                case 6:
                    structWriter.WriteField(nameof(ne_flags), FlagsOffset, ne_flags, sizeof(short));
                    break;

                case 7:
                    structWriter.WriteField(nameof(ne_autodata), AutomaticDataSegmentNumberOffset, ne_autodata);
                    break;

                case 8:
                    structWriter.WriteField(nameof(ne_heap), InitialHeapAllocationOffset, ne_heap);
                    break;

                case 9:
                    structWriter.WriteField(nameof(ne_stack), InitialStackAllocationOffset, ne_stack);
                    break;

                case 10:
                    structWriter.WriteField(nameof(ne_csip), InitialCSIPSettingOffset, ne_csip);
                    break;

                case 11:
                    structWriter.WriteField(nameof(ne_sssp), InitialSSSPSettingOffset, ne_sssp);
                    break;

                case 12:
                    structWriter.WriteField(nameof(ne_cseg), CountOfFileSegmentsOffset, ne_cseg);
                    break;

                case 13:
                    structWriter.WriteField(nameof(ne_cmod), EntriesInModuleReferenceTableOffset, ne_cmod);
                    break;

                case 14:
                    structWriter.WriteField(nameof(ne_cbnrestab), SizeOfNonResidentNameTableOffset, ne_cbnrestab);
                    break;

                case 15:
                    structWriter.WriteField(nameof(ne_segtab), OffsetOfSegmentTableOffset, ne_segtab);
                    break;

                case 16:
                    structWriter.WriteField(nameof(ne_rsrctab), OffsetOfResourceTableOffset, ne_rsrctab);
                    break;

                case 17:
                    structWriter.WriteField(nameof(ne_restab), OffsetOfResidentNameTableOffset, ne_restab);
                    break;

                case 18:
                    structWriter.WriteField(nameof(ne_modtab), OffsetOfModuleReferenceTableOffset, ne_modtab);
                    break;

                case 19:
                    structWriter.WriteField(nameof(ne_imptab), OffsetOfImportedNamesTableOffset, ne_imptab);
                    break;

                case 20:
                    structWriter.WriteField(nameof(ne_nrestab), OffsetOfNonResidentNamesTableOffset, ne_nrestab);
                    break;

                case 21:
                    structWriter.WriteField(nameof(ne_cmovent), CountOfMovableEntriesOffset, ne_cmovent);
                    break;

                case 22:
                    structWriter.WriteField(nameof(ne_align), SegmentAlignmentShiftCountOffset, ne_align);
                    break;

                case 23:
                    structWriter.WriteField(nameof(ne_cres), CountOfResourceSegmentsOffset, ne_cres);
                    break;

                case 24:
                    structWriter.WriteField(nameof(ne_exetyp), TargetOperatingSystemOffset, ne_exetyp, sizeof(byte));
                    break;

                case 25:
                    structWriter.WriteField(nameof(ne_flagsothers), OtherExeFlagsOffset, ne_flagsothers, sizeof(byte));
                    break;

                case 26:
                    structWriter.WriteField(nameof(ne_pretthunks), OffsetToReturnThunksOffset, ne_pretthunks);
                    break;

                case 27:
                    structWriter.WriteField(nameof(ne_psegrefbytes), OffsetToSegmentRefBytesOffset, ne_psegrefbytes);
                    break;

                case 28:
                    structWriter.WriteField(nameof(ne_swaparea), MinimumCodeSwapAreaSizeOffset, ne_swaparea);
                    break;

                case 29:
                    structWriter.WriteField(nameof(ne_expver), ExpectedWindowsVersionNumberOffset, ne_expver);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
