using System;
using System.Diagnostics;
using PESpy.LE;
using PESpy.NE;
using PESpy.View;

namespace PESpy
{
    //Also called e32_exe
    public readonly struct ImageVXDHeader : IValue, IViewable
    {
        public const ushort IMAGE_VXD_SIGNATURE = 0x454C;      // LE

        private const int MagicOffset = 0;
        private const int ByteOrderOffset = 2;
        private const int WordOrderOffset = 3;
        private const int ExeFormatLevelOffset = 4;
        private const int CpuTypeOffset = 8;
        private const int OSTypeOffset = 10;
        private const int ModuleVersionOffset = 12;
        private const int ModuleFlagsOffset = 16;
        private const int NumModulePagesOffset = 20;
        private const int ObjectNumForIPOffset = 24;
        private const int EIPOffset = 28;
        private const int ObjectNumForSPOffset = 32;
        private const int ESPOffset = 36;
        private const int PageSizeOffset = 40;
        private const int LastPageSizeOffset = 44;
        private const int FixupSectionSizeOffset = 48;
        private const int FixupSectionChecksumOffset = 52;
        private const int LoaderSectionSizeOffset = 56;
        private const int LoaderSectionChecksumOffset = 60;
        private const int OffsetOfObjectTableOffset = 64;
        private const int NumObjectsInModuleOffset = 68;
        private const int OffsetOfObjectPageMapOffset = 72;
        private const int OffsetOfIteratedDataMapOffset = 76;
        private const int OffsetOfResourceTableOffset = 80;
        private const int NumResourceEntriesOffset = 84;
        private const int OffsetOfResidentNameTableOffset = 88;
        private const int OffsetOfEntryTableOffset = 92;
        private const int OffsetOfModuleDirectiveTableOffset = 96;
        private const int NumModuleDirectivesOffset = 100;
        private const int OffsetOfFixupPageTableOffset = 104;
        private const int OffsetOfFixupRecordTableOffset = 108;
        private const int OffsetOfImportModuleNameTableOffset = 112;
        private const int NumImportModuleNameTableEntriesOffset = 116;
        private const int OffsetOfImportProcedureNameTableOffset = 120;
        private const int OffsetOfPerPageChecksumTableOffset = 124;
        private const int OffsetOfEnumeratedDataPagesOffset = 128;
        private const int NumPreloadPagesOffset = 132;
        private const int OffsetOfNonResidentNamesTableOffset = 136;
        private const int SizeOfNonResidentNameTableOffset = 140;
        private const int NonResidentNameTableChecksumOffset = 144;
        private const int ObjectNumForAutomaticDataObjectOffset = 148;
        private const int OffsetOfDebugInfoOffset = 152;
        private const int DebugInfoLengthOffset = 156;
        private const int NumPreloadSectionInstancePagesOffset = 160;
        private const int NumDemandLoadInstancePagesOffset = 164;
        private const int HeapSizeOffset = 168;
        private const int ReservedOffset = 172;
        private const int e32_winresoffOffset = 184;
        private const int e32_winreslenOffset = 188;
        private const int DeviceIDOffset = 192;
        private const int DDKVersionOffset = 194;

        /// <summary>
        /// Magic number<para/>
        /// e32_magic
        /// </summary>
        public ushort Magic => chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// The byte ordering for the VXD<para/>
        /// e32_border
        /// </summary>
        public E32ByteOrder ByteOrder => (E32ByteOrder) chunk.PeekByte(ByteOrderOffset);

        /// <summary>
        /// The word ordering for the VXD<para/>
        /// e32_worder
        /// </summary>
        public E32WordOrder WordOrder => (E32WordOrder) chunk.PeekByte(WordOrderOffset);

        /// <summary>
        /// The EXE format level for now = 0<para/>
        /// e32_level
        /// </summary>
        public E32Level ExeFormatLevel => (E32Level) chunk.PeekUInt32(ExeFormatLevelOffset);

        /// <summary>
        /// The CPU type<para/>
        /// e32_cpu
        /// </summary>
        public E32CPU CpuType => (E32CPU) chunk.PeekUInt16(CpuTypeOffset);

        /// <summary>
        /// The OS type<para/>
        /// e32_os
        /// </summary>
        public NewOperatingSystem OSType => (NewOperatingSystem) chunk.PeekUInt16(OSTypeOffset);

        /// <summary>
        /// Module version<para/>
        /// e32_ver
        /// </summary>
        public int ModuleVersion => chunk.PeekInt32(ModuleVersionOffset);

        /// <summary>
        /// Module flags<para/>
        /// e32_mflags
        /// </summary>
        public E32ModuleFlags ModuleFlags => (E32ModuleFlags) chunk.PeekUInt32(ModuleFlagsOffset);

        /// <summary>
        /// Module # pages<para/>
        /// e32_mpages
        /// </summary>
        public int NumModulePages => chunk.PeekInt32(NumModulePagesOffset);

        /// <summary>
        /// Object # for instruction pointer<para/>
        /// e32_startobj
        /// </summary>
        public int ObjectNumForIP => chunk.PeekInt32(ObjectNumForIPOffset);

        /// <summary>
        /// Extended instruction pointer<para/>
        /// e32_eip
        /// </summary>
        public int EIP => chunk.PeekInt32(EIPOffset);

        /// <summary>
        /// Object # for stack pointer<para/>
        /// e32_stackobj
        /// </summary>
        public int ObjectNumForSP => chunk.PeekInt32(ObjectNumForSPOffset);

        /// <summary>
        /// Extended stack pointer<para/>
        /// e32_esp
        /// </summary>
        public int ESP => chunk.PeekInt32(ESPOffset);

        /// <summary>
        /// VXD page size<para/>
        /// e32_pagesize
        /// </summary>
        public int PageSize => chunk.PeekInt32(PageSizeOffset);

        /// <summary>
        /// Last page size in VXD<para/>
        /// e32_lastpagesize
        /// </summary>
        public int LastPageSize => chunk.PeekInt32(LastPageSizeOffset);

        /// <summary>
        /// Fixup section size<para/>
        /// e32_fixupsize
        /// </summary>
        public int FixupSectionSize => chunk.PeekInt32(FixupSectionSizeOffset);

        /// <summary>
        /// Fixup section checksum<para/>
        /// e32_fixupsum
        /// </summary>
        public int FixupSectionChecksum => chunk.PeekInt32(FixupSectionChecksumOffset);

        /// <summary>
        /// Loader section size<para/>
        /// e32_ldrsize
        /// </summary>
        public int LoaderSectionSize => chunk.PeekInt32(LoaderSectionSizeOffset);

        /// <summary>
        /// Loader section checksum<para/>
        /// e32_ldrsum
        /// </summary>
        public int LoaderSectionChecksum => chunk.PeekInt32(LoaderSectionChecksumOffset);

        /// <summary>
        /// Object table offset<para/>
        /// e32_objtab
        /// </summary>
        public int OffsetOfObjectTable => chunk.PeekInt32(OffsetOfObjectTableOffset); //Relative to LE header

        /// <summary>
        /// Number of objects in module<para/>
        /// e32_objcnt
        /// </summary>
        public int NumObjectsInModule => chunk.PeekInt32(NumObjectsInModuleOffset);

        /// <summary>
        /// Object page map offset<para/>
        /// e32_objmap
        /// </summary>
        public int OffsetOfObjectPageMap => chunk.PeekInt32(OffsetOfObjectPageMapOffset); //Relative to LE header

        /// <summary>
        /// Object iterated data map offset<para/>
        /// e32_itermap
        /// </summary>
        public int OffsetOfIteratedDataMap => chunk.PeekInt32(OffsetOfIteratedDataMapOffset); //Relative to beginning of file

        /// <summary>
        /// Offset of Resource Table<para/>
        /// e32_rsrctab
        /// </summary>
        public int OffsetOfResourceTable => chunk.PeekInt32(OffsetOfResourceTableOffset); //Relative to LE header

        /// <summary>
        /// Number of resource entries<para/>
        /// e32_rsrccnt
        /// </summary>
        public int NumResourceEntries => chunk.PeekInt32(NumResourceEntriesOffset);

        /// <summary>
        /// Offset of resident name table<para/>
        /// e32_restab
        /// </summary>
        public int OffsetOfResidentNameTable => chunk.PeekInt32(OffsetOfResidentNameTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Entry Table<para/>
        /// e32_enttab
        /// </summary>
        public int OffsetOfEntryTable => chunk.PeekInt32(OffsetOfEntryTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Module Directive Table<para/>
        /// e32_dirtab
        /// </summary>
        public int OffsetOfModuleDirectiveTable => chunk.PeekInt32(OffsetOfModuleDirectiveTableOffset); //Relative to LE header

        /// <summary>
        /// Number of module directives<para/>
        /// e32_dircnt
        /// </summary>
        public int NumModuleDirectives => chunk.PeekInt32(NumModuleDirectivesOffset);

        /// <summary>
        /// Offset of Fixup Page Table<para/>
        /// e32_fpagetab
        /// </summary>
        public int OffsetOfFixupPageTable => chunk.PeekInt32(OffsetOfFixupPageTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Fixup Record Table<para/>
        /// e32_frectab
        /// </summary>
        public int OffsetOfFixupRecordTable => chunk.PeekInt32(OffsetOfFixupRecordTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Import Module Name Table<para/>
        /// e32_impmod
        /// </summary>
        public int OffsetOfImportModuleNameTable => chunk.PeekInt32(OffsetOfImportModuleNameTableOffset); //Relative to LE header

        /// <summary>
        /// Number of entries in Import Module Name Table<para/>
        /// e32_impmodcnt
        /// </summary>
        public int NumImportModuleNameTableEntries => chunk.PeekInt32(NumImportModuleNameTableEntriesOffset);

        /// <summary>
        /// Offset of Import Procedure Name Table<para/>
        /// e32_impproc
        /// </summary>
        public int OffsetOfImportProcedureNameTable => chunk.PeekInt32(OffsetOfImportProcedureNameTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Per-Page Checksum Table<para/>
        /// e32_pagesum
        /// </summary>
        public int OffsetOfPerPageChecksumTable => chunk.PeekInt32(OffsetOfPerPageChecksumTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Enumerated Data Pages<para/>
        /// e32_datapage
        /// </summary>
        public int OffsetOfEnumeratedDataPages => chunk.PeekInt32(OffsetOfEnumeratedDataPagesOffset); //Relative to beginning of file

        /// <summary>
        /// Number of preload pages<para/>
        /// e32_preload
        /// </summary>
        public int NumPreloadPages => chunk.PeekInt32(NumPreloadPagesOffset);

        /// <summary>
        /// Offset of Non-resident Names Table<para/>
        /// e32_nrestab
        /// </summary>
        public int OffsetOfNonResidentNamesTable => chunk.PeekInt32(OffsetOfNonResidentNamesTableOffset); //Relative to beginning of file

        /// <summary>
        /// Size of Non-resident Name Table<para/>
        /// e32_cbnrestab
        /// </summary>
        public int SizeOfNonResidentNameTable => chunk.PeekInt32(SizeOfNonResidentNameTableOffset);

        /// <summary>
        /// Non-resident Name Table Checksum<para/>
        /// e32_nressum
        /// </summary>
        public int NonResidentNameTableChecksum => chunk.PeekInt32(NonResidentNameTableChecksumOffset); //Relative to LE header

        /// <summary>
        /// Object # for automatic data object<para/>
        /// e32_autodata
        /// </summary>
        public int ObjectNumForAutomaticDataObject => chunk.PeekInt32(ObjectNumForAutomaticDataObjectOffset);

        /// <summary>
        /// Offset of the debugging information<para/>
        /// e32_debuginfo
        /// </summary>
        public int OffsetOfDebugInfo => chunk.PeekInt32(OffsetOfDebugInfoOffset); //Relative to LE header

        /// <summary>
        /// The length of the debugging info. in bytes<para/>
        /// e32_debuglen
        /// </summary>
        public int DebugInfoLength => chunk.PeekInt32(DebugInfoLengthOffset);

        /// <summary>
        /// Number of instance pages in preload section of VXD file<para/>
        /// e32_instpreload
        /// </summary>
        public int NumPreloadSectionInstancePages => chunk.PeekInt32(NumPreloadSectionInstancePagesOffset);

        /// <summary>
        /// Number of instance pages in demand load section of VXD file<para/>
        /// e32_instdemand
        /// </summary>
        public int NumDemandLoadInstancePages => chunk.PeekInt32(NumDemandLoadInstancePagesOffset);

        /// <summary>
        /// Size of heap - for 16-bit apps<para/>
        /// e32_heapsize
        /// </summary>
        public int HeapSize => chunk.PeekInt32(HeapSizeOffset);

        /// <summary>
        /// Reserved words<para/>
        /// e32_res3
        /// </summary>
        public NativeSpan<byte> Reserved => chunk.PeekNativeSpan<byte>(ReservedOffset, 12);

        public int e32_winresoff => chunk.PeekInt32(e32_winresoffOffset);

        public int e32_winreslen => chunk.PeekInt32(e32_winreslenOffset);

        /// <summary>
        /// Device ID for VxD<para/>
        /// e32_devid
        /// </summary>
        public ushort DeviceID => chunk.PeekUInt16(DeviceIDOffset);

        /// <summary>
        /// DDK version for VxD<para/>
        /// e32_ddkver
        /// </summary>
        public ushort DDKVersion => chunk.PeekUInt16(DDKVersionOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(ushort) + //e32_magic
            sizeof(byte) +   //e32_border
            sizeof(byte) +   //e32_worder
            sizeof(int) +    //e32_level
            sizeof(ushort) + //e32_cpu
            sizeof(ushort) + //e32_os
            sizeof(int) +    //e32_ver
            sizeof(int) +    //e32_mflags
            sizeof(int) +    //e32_mpages
            sizeof(int) +    //e32_startobj
            sizeof(int) +    //e32_eip
            sizeof(int) +    //e32_stackobj
            sizeof(int) +    //e32_esp
            sizeof(int) +    //e32_pagesize
            sizeof(int) +    //e32_lastpagesize
            sizeof(int) +    //e32_fixupsize
            sizeof(int) +    //e32_fixupsum
            sizeof(int) +    //e32_ldrsize
            sizeof(int) +    //e32_ldrsum
            sizeof(int) +    //e32_objtab
            sizeof(int) +    //e32_objcnt
            sizeof(int) +    //e32_objmap
            sizeof(int) +    //e32_itermap
            sizeof(int) +    //e32_rsrctab
            sizeof(int) +    //e32_rsrccnt
            sizeof(int) +    //e32_restab
            sizeof(int) +    //e32_enttab
            sizeof(int) +    //e32_dirtab
            sizeof(int) +    //e32_dircnt
            sizeof(int) +    //e32_fpagetab
            sizeof(int) +    //e32_frectab
            sizeof(int) +    //e32_impmod
            sizeof(int) +    //e32_impmodcnt
            sizeof(int) +    //e32_impproc
            sizeof(int) +    //e32_pagesum
            sizeof(int) +    //e32_datapage
            sizeof(int) +    //e32_preload
            sizeof(int) +    //e32_nrestab
            sizeof(int) +    //e32_cbnrestab
            sizeof(int) +    //e32_nressum
            sizeof(int) +    //e32_autodata
            sizeof(int) +    //e32_debuginfo
            sizeof(int) +    //e32_debuglen
            sizeof(int) +    //e32_instpreload
            sizeof(int) +    //e32_instdemand
            sizeof(int) +    //e32_heapsize
            12 +             //byte e32_res3[12]
            sizeof(int) +    //e32_winresoff
            sizeof(int) +    //e32_winreslen
            sizeof(ushort) + //e32_devid
            sizeof(ushort);  //e32_ddkver

        private readonly MemoryChunk chunk;

        internal ImageVXDHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_VXD_HEADER, this, ViewKind.ImageVXDHeader, StructSize);

        int IViewable.NumChildren() => 51;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("e32_magic", MagicOffset, Magic);
                    break;

                case 1:
                    structWriter.WriteField("e32_border", ByteOrderOffset, ByteOrder, sizeof(byte));
                    break;

                case 2:
                    structWriter.WriteField("e32_worder", WordOrderOffset, WordOrder, sizeof(byte));
                    break;

                case 3:
                    structWriter.WriteField("e32_level", ExeFormatLevelOffset, ExeFormatLevel, sizeof(int));
                    break;

                case 4:
                    structWriter.WriteField("e32_cpu", CpuTypeOffset, CpuType, sizeof(short));
                    break;

                case 5:
                    structWriter.WriteField("e32_os", OSTypeOffset, OSType, sizeof(ushort));
                    break;

                case 6:
                    structWriter.WriteField("e32_ver", ModuleVersionOffset, ModuleVersion);
                    break;

                case 7:
                    structWriter.WriteField("e32_mflags", ModuleFlagsOffset, ModuleFlags, sizeof(int));
                    break;

                case 8:
                    structWriter.WriteField("e32_mpages", NumModulePagesOffset, NumModulePages);
                    break;

                case 9:
                    structWriter.WriteField("e32_startobj", ObjectNumForIPOffset, ObjectNumForIP);
                    break;

                case 10:
                    structWriter.WriteField("e32_eip", EIPOffset, EIP);
                    break;

                case 11:
                    structWriter.WriteField("e32_stackobj", ObjectNumForSPOffset, ObjectNumForSP);
                    break;

                case 12:
                    structWriter.WriteField("e32_esp", ESPOffset, ESP);
                    break;

                case 13:
                    structWriter.WriteField("e32_pagesize", PageSizeOffset, PageSize);
                    break;

                case 14:
                    structWriter.WriteField("e32_lastpagesize", LastPageSizeOffset, LastPageSize);
                    break;

                case 15:
                    structWriter.WriteField("e32_fixupsize", FixupSectionSizeOffset, FixupSectionSize);
                    break;

                case 16:
                    structWriter.WriteField("e32_fixupsum", FixupSectionChecksumOffset, FixupSectionChecksum);
                    break;

                case 17:
                    structWriter.WriteField("e32_ldrsize", LoaderSectionSizeOffset, LoaderSectionSize);
                    break;

                case 18:
                    structWriter.WriteField("e32_ldrsum", LoaderSectionChecksumOffset, LoaderSectionChecksum);
                    break;

                case 19:
                    structWriter.WriteField("e32_objtab", OffsetOfObjectTableOffset, OffsetOfObjectTable);
                    break;

                case 20:
                    structWriter.WriteField("e32_objcnt", NumObjectsInModuleOffset, NumObjectsInModule);
                    break;

                case 21:
                    structWriter.WriteField("e32_objmap", OffsetOfObjectPageMapOffset, OffsetOfObjectPageMap);
                    break;

                case 22:
                    structWriter.WriteField("e32_itermap", OffsetOfIteratedDataMapOffset, OffsetOfIteratedDataMap);
                    break;

                case 23:
                    structWriter.WriteField("e32_rsrctab", OffsetOfResourceTableOffset, OffsetOfResourceTable);
                    break;

                case 24:
                    structWriter.WriteField("e32_rsrccnt", NumResourceEntriesOffset, NumResourceEntries);
                    break;

                case 25:
                    structWriter.WriteField("e32_restab", OffsetOfResidentNameTableOffset, OffsetOfResidentNameTable);
                    break;

                case 26:
                    structWriter.WriteField("e32_enttab", OffsetOfEntryTableOffset, OffsetOfEntryTable);
                    break;

                case 27:
                    structWriter.WriteField("e32_dirtab", OffsetOfModuleDirectiveTableOffset, OffsetOfModuleDirectiveTable);
                    break;

                case 28:
                    structWriter.WriteField("e32_dircnt", NumModuleDirectivesOffset, NumModuleDirectives);
                    break;

                case 29:
                    structWriter.WriteField("e32_fpagetab", OffsetOfFixupPageTableOffset, OffsetOfFixupPageTable);
                    break;

                case 30:
                    structWriter.WriteField("e32_frectab", OffsetOfFixupRecordTableOffset, OffsetOfFixupRecordTable);
                    break;

                case 31:
                    structWriter.WriteField("e32_impmod", OffsetOfImportModuleNameTableOffset, OffsetOfImportModuleNameTable);
                    break;

                case 32:
                    structWriter.WriteField("e32_impmodcnt", NumImportModuleNameTableEntriesOffset, NumImportModuleNameTableEntries);
                    break;

                case 33:
                    structWriter.WriteField("e32_impproc", OffsetOfImportProcedureNameTableOffset, OffsetOfImportProcedureNameTable);
                    break;

                case 34:
                    structWriter.WriteField("e32_pagesum", OffsetOfPerPageChecksumTableOffset, OffsetOfPerPageChecksumTable);
                    break;

                case 35:
                    structWriter.WriteField("e32_datapage", OffsetOfEnumeratedDataPagesOffset, OffsetOfEnumeratedDataPages);
                    break;

                case 36:
                    structWriter.WriteField("e32_preload", NumPreloadPagesOffset, NumPreloadPages);
                    break;

                case 37:
                    structWriter.WriteField("e32_nrestab", OffsetOfNonResidentNamesTableOffset, OffsetOfNonResidentNamesTable);
                    break;

                case 38:
                    structWriter.WriteField("e32_cbnrestab", SizeOfNonResidentNameTableOffset, SizeOfNonResidentNameTable);
                    break;

                case 39:
                    structWriter.WriteField("e32_nressum", NonResidentNameTableChecksumOffset, NonResidentNameTableChecksum);
                    break;

                case 40:
                    structWriter.WriteField("e32_autodata", ObjectNumForAutomaticDataObjectOffset, ObjectNumForAutomaticDataObject);
                    break;

                case 41:
                    structWriter.WriteField("e32_debuginfo", OffsetOfDebugInfoOffset, OffsetOfDebugInfo);
                    break;

                case 42:
                    structWriter.WriteField("e32_debuglen", DebugInfoLengthOffset, DebugInfoLength);
                    break;

                case 43:
                    structWriter.WriteField("e32_instpreload", NumPreloadSectionInstancePagesOffset, NumPreloadSectionInstancePages);
                    break;

                case 44:
                    structWriter.WriteField("e32_instdemand", NumDemandLoadInstancePagesOffset, NumDemandLoadInstancePages);
                    break;

                case 45:
                    structWriter.WriteField("e32_heapsize", HeapSizeOffset, HeapSize);
                    break;

                case 46:
                    structWriter.WriteField("e32_res3", ReservedOffset, Reserved);
                    break;

                case 47:
                    structWriter.WriteField("e32_winresoff", e32_winresoffOffset, e32_winresoff);
                    break;

                case 48:
                    structWriter.WriteField("e32_winreslen", e32_winreslenOffset, e32_winreslen);
                    break;

                case 49:
                    structWriter.WriteField("e32_devid", DeviceIDOffset, DeviceID);
                    break;

                case 50:
                    structWriter.WriteField("e32_ddkver", DDKVersionOffset, DDKVersion);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
