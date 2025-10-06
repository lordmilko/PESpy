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

        /// <summary>
        /// Magic number<para/>
        /// ne_magic
        /// </summary>
        public ushort Magic => chunk.PeekUInt16(0);

        /// <summary>
        /// Version number (major version number of LINK.EXE)<para/>
        /// ne_ver
        /// </summary>
        public byte VersionNumber => chunk.PeekByte(2);

        /// <summary>
        /// Revision number (minor version number of LINK.EXE)<para/>
        /// ne_rev
        /// </summary>
        public byte RevisionNumber => chunk.PeekByte(3);

        /// <summary>
        /// Offset of Entry Table<para/>
        /// ne_enttab
        /// </summary>
        public ushort OffsetOfEntryTable => chunk.PeekUInt16(4);

        /// <summary>
        /// Number of bytes in Entry Table<para/>
        /// ne_cbenttab
        /// </summary>
        public ushort NumberOfBytesInEntryTable => chunk.PeekUInt16(6);

        /// <summary>
        /// Checksum of whole file<para/>
        /// ne_crc
        /// </summary>
        public int ChecksumOfWholeFile => chunk.PeekInt32(8);

        /// <summary>
        /// Flag word<para/>
        /// ne_flags
        /// </summary>
        public NewExeFlags Flags => (NewExeFlags) chunk.PeekUInt16(12);

        /// <summary>
        /// Automatic data segment number<para/>
        /// ne_autodata
        /// </summary>
        public ushort AutomaticDataSegmentNumber => chunk.PeekUInt16(14);

        /// <summary>
        /// Initial heap allocation<para/>
        /// ne_heap
        /// </summary>
        public ushort InitialHeapAllocation => chunk.PeekUInt16(16);

        /// <summary>
        /// Initial stack allocation<para/>
        /// ne_stack
        /// </summary>
        public ushort InitialStackAllocation => chunk.PeekUInt16(18);

        /// <summary>
        /// Initial CS:IP setting<para/>
        /// ne_csip
        /// </summary>
        public int InitialCSIPSetting => chunk.PeekInt32(20);

        /// <summary>
        /// Initial SS:SP setting<para/>
        /// ne_sssp
        /// </summary>
        public int InitialSSSPSetting => chunk.PeekInt32(24);

        /// <summary>
        /// Count of file segments<para/>
        /// ne_cseg
        /// </summary>
        public ushort CountOfFileSegments => chunk.PeekUInt16(28);

        /// <summary>
        /// Entries in Module Reference Table<para/>
        /// ne_cmod
        /// </summary>
        public ushort EntriesInModuleReferenceTable => chunk.PeekUInt16(30);

        /// <summary>
        /// Size of non-resident name table<para/>
        /// ne_cbnrestab
        /// </summary>
        public ushort SizeOfNonResidentNameTable => chunk.PeekUInt16(32);

        //I think a common pattern that is used is to say that, if the offset of a given table entry is equal to the offset
        //of the entry after it, that entry does not exist

        /// <summary>
        /// Offset of Segment Table<para/>
        /// ne_segtab
        /// </summary>
        public ushort OffsetOfSegmentTable => chunk.PeekUInt16(34);

        /// <summary>
        /// Offset of Resource Table<para/>
        /// If this value is equal to <see cref="OffsetOfResidentNameTable"/> (ne_rsrctab == ne_restab) there are no resources.
        /// ne_rsrctab
        /// </summary>
        public ushort OffsetOfResourceTable => chunk.PeekUInt16(36);

        /// <summary>
        /// Offset of resident name table<para/>
        /// ne_restab
        /// </summary>
        public ushort OffsetOfResidentNameTable => chunk.PeekUInt16(38);

        /// <summary>
        /// Offset of Module Reference Table<para/>
        /// ne_modtab
        /// </summary>
        public ushort OffsetOfModuleReferenceTable => chunk.PeekUInt16(40);

        /// <summary>
        /// Offset of Imported Names Table<para/>
        /// ne_imptab
        /// </summary>
        public ushort OffsetOfImportedNamesTable => chunk.PeekUInt16(42);

        /// <summary>
        /// Offset of Non-resident Names Table<para/>
        /// ne_nrestab
        /// </summary>
        public int OffsetOfNonResidentNamesTable => chunk.PeekInt32(44);

        /// <summary>
        /// Count of movable entries<para/>
        /// ne_cmovent
        /// </summary>
        public ushort CountOfMovableEntries => chunk.PeekUInt16(48);

        /// <summary>
        /// Segment alignment shift count<para/>
        /// ne_align
        /// </summary>
        public ushort SegmentAlignmentShiftCount => chunk.PeekUInt16(50);

        /// <summary>
        /// Count of resource segments<para/>
        /// ne_cres
        /// </summary>
        public ushort CountOfResourceSegments => chunk.PeekUInt16(52);

        /// <summary>
        /// Target Operating system<para/>
        /// ne_exetyp
        /// </summary>
        public NewOperatingSystem TargetOperatingSystem => (NewOperatingSystem) chunk.PeekByte(54);

        /// <summary>
        /// Other .EXE flags<para/>
        /// ne_flagsothers
        /// </summary>
        public NewOtherExeFlags OtherExeFlags => (NewOtherExeFlags) chunk.PeekByte(55);

        /// <summary>
        /// offset to return thunks<para/>
        /// ne_pretthunks
        /// </summary>
        public ushort OffsetToReturnThunks => chunk.PeekUInt16(56);

        /// <summary>
        /// offset to segment ref. bytes<para/>
        /// ne_psegrefbytes
        /// </summary>
        public ushort OffsetToSegmentRefBytes => chunk.PeekUInt16(58);

        /// <summary>
        /// Minimum code swap area size<para/>
        /// ne_swaparea
        /// </summary>
        public ushort MinimumCodeSwapAreaSize => chunk.PeekUInt16(60);

        /// <summary>
        /// Expected Windows version number<para/>
        /// ne_expver
        /// </summary>
        public ushort ExpectedWindowsVersionNumber => chunk.PeekUInt16(62);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("ne_magic", Magic);
            s.WriteField("ne_ver", VersionNumber);
            s.WriteField("ne_rev", RevisionNumber);
            s.WriteField("ne_enttab", OffsetOfEntryTable);
            s.WriteField("ne_cbenttab", NumberOfBytesInEntryTable);
            s.WriteField("ne_crc", ChecksumOfWholeFile);
            s.WriteField("ne_flags", Flags, sizeof(short));
            s.WriteField("ne_autodata", AutomaticDataSegmentNumber);
            s.WriteField("ne_heap", InitialHeapAllocation);
            s.WriteField("ne_stack", InitialStackAllocation);
            s.WriteField("ne_csip", InitialCSIPSetting);
            s.WriteField("ne_sssp", InitialSSSPSetting);
            s.WriteField("ne_cseg", CountOfFileSegments);
            s.WriteField("ne_cmod", EntriesInModuleReferenceTable);
            s.WriteField("ne_cbnrestab", SizeOfNonResidentNameTable);
            s.WriteField("ne_segtab", OffsetOfSegmentTable);
            s.WriteField("ne_rsrctab", OffsetOfResourceTable);
            s.WriteField("ne_restab", OffsetOfResidentNameTable);
            s.WriteField("ne_modtab", OffsetOfModuleReferenceTable);
            s.WriteField("ne_imptab", OffsetOfImportedNamesTable);
            s.WriteField("ne_nrestab", OffsetOfNonResidentNamesTable);
            s.WriteField("ne_cmovent", CountOfMovableEntries);
            s.WriteField("ne_align", SegmentAlignmentShiftCount);
            s.WriteField("ne_cres", CountOfResourceSegments);
            s.WriteField("ne_exetyp", TargetOperatingSystem, sizeof(byte));
            s.WriteField("ne_flagsothers", OtherExeFlags, sizeof(byte));
            s.WriteField("ne_pretthunks", OffsetToReturnThunks);
            s.WriteField("ne_psegrefbytes", OffsetToSegmentRefBytes);
            s.WriteField("ne_swaparea", MinimumCodeSwapAreaSize);
            s.WriteField("ne_expver", ExpectedWindowsVersionNumber);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
