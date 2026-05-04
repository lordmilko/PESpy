using System;
using System.Diagnostics;
using PESpy.LE;
using PESpy.NE;
using PESpy.View;

namespace PESpy
{
    //Also called e32_exe (exe_vhd.h. Note that the definition in exe386.h is not correct)
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
        /// Magic number
        /// </summary>
        public ushort e32_magic => chunk.PeekUInt16(MagicOffset);

        /// <summary>
        /// The byte ordering for the VXD
        /// </summary>
        public E32ByteOrder e32_border => (E32ByteOrder) chunk.PeekByte(ByteOrderOffset);

        /// <summary>
        /// The word ordering for the VXD
        /// </summary>
        public E32WordOrder e32_worder => (E32WordOrder) chunk.PeekByte(WordOrderOffset);

        /// <summary>
        /// The EXE format level for now = 0
        /// </summary>
        public E32Level e32_level => (E32Level) chunk.PeekUInt32(ExeFormatLevelOffset);

        /// <summary>
        /// The CPU type
        /// </summary>
        public E32CPU e32_cpu => (E32CPU) chunk.PeekUInt16(CpuTypeOffset);

        /// <summary>
        /// The OS type
        /// </summary>
        public NewOperatingSystem e32_os => (NewOperatingSystem) chunk.PeekUInt16(OSTypeOffset);

        /// <summary>
        /// Module version
        /// </summary>
        public int e32_ver => chunk.PeekInt32(ModuleVersionOffset);

        /// <summary>
        /// Module flags
        /// </summary>
        public E32ModuleFlags e32_mflags => (E32ModuleFlags) chunk.PeekUInt32(ModuleFlagsOffset);

        /// <summary>
        /// Module # pages
        /// </summary>
        public int e32_mpages => chunk.PeekInt32(NumModulePagesOffset);

        /// <summary>
        /// Object # for instruction pointer
        /// </summary>
        public int e32_startobj => chunk.PeekInt32(ObjectNumForIPOffset); //Refers to the 1-based index into LEFile.ObjectTable (i.e. "2" -> index 1)

        /// <summary>
        /// Extended instruction pointer
        /// </summary>
        public int e32_eip => chunk.PeekInt32(EIPOffset);

        /// <summary>
        /// Object # for stack pointer
        /// </summary>
        public int e32_stackobj => chunk.PeekInt32(ObjectNumForSPOffset);

        /// <summary>
        /// Extended stack pointer
        /// </summary>
        public int e32_esp => chunk.PeekInt32(ESPOffset);

        /// <summary>
        /// VXD page size
        /// </summary>
        public int e32_pagesize => chunk.PeekInt32(PageSizeOffset);

        /// <summary>
        /// Last page size in VXD
        /// </summary>
        public int e32_lastpagesize => chunk.PeekInt32(LastPageSizeOffset);

        /// <summary>
        /// Fixup section size
        /// </summary>
        public int e32_fixupsize => chunk.PeekInt32(FixupSectionSizeOffset);

        /// <summary>
        /// Fixup section checksum
        /// </summary>
        public int e32_fixupsum => chunk.PeekInt32(FixupSectionChecksumOffset);

        /// <summary>
        /// Loader section size
        /// </summary>
        public int e32_ldrsize => chunk.PeekInt32(LoaderSectionSizeOffset);

        /// <summary>
        /// Loader section checksum
        /// </summary>
        public int e32_ldrsum => chunk.PeekInt32(LoaderSectionChecksumOffset);

        /// <summary>
        /// Object table offset
        /// </summary>
        public int e32_objtab => chunk.PeekInt32(OffsetOfObjectTableOffset); //Relative to LE header

        /// <summary>
        /// Number of objects in module
        /// </summary>
        public int e32_objcnt => chunk.PeekInt32(NumObjectsInModuleOffset);

        /// <summary>
        /// Object page map offset
        /// </summary>
        public int e32_objmap => chunk.PeekInt32(OffsetOfObjectPageMapOffset); //Relative to LE header

        /// <summary>
        /// Object iterated data map offset
        /// </summary>
        public int e32_itermap => chunk.PeekInt32(OffsetOfIteratedDataMapOffset); //Relative to beginning of file

        /// <summary>
        /// Offset of Resource Table
        /// </summary>
        public int e32_rsrctab => chunk.PeekInt32(OffsetOfResourceTableOffset); //Relative to LE header

        /// <summary>
        /// Number of resource entries
        /// </summary>
        public int e32_rsrccnt => chunk.PeekInt32(NumResourceEntriesOffset);

        /// <summary>
        /// Offset of resident name table
        /// </summary>
        public int e32_restab => chunk.PeekInt32(OffsetOfResidentNameTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Entry Table
        /// </summary>
        public int e32_enttab => chunk.PeekInt32(OffsetOfEntryTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Module Directive Table
        /// </summary>
        public int e32_dirtab => chunk.PeekInt32(OffsetOfModuleDirectiveTableOffset); //Relative to LE header

        /// <summary>
        /// Number of module directives
        /// </summary>
        public int e32_dircnt => chunk.PeekInt32(NumModuleDirectivesOffset);

        /// <summary>
        /// Offset of Fixup Page Table
        /// </summary>
        public int e32_fpagetab => chunk.PeekInt32(OffsetOfFixupPageTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Fixup Record Table
        /// </summary>
        public int e32_frectab => chunk.PeekInt32(OffsetOfFixupRecordTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Import Module Name Table
        /// </summary>
        public int e32_impmod => chunk.PeekInt32(OffsetOfImportModuleNameTableOffset); //Relative to LE header

        /// <summary>
        /// Number of entries in Import Module Name Table
        /// </summary>
        public int e32_impmodcnt => chunk.PeekInt32(NumImportModuleNameTableEntriesOffset);

        /// <summary>
        /// Offset of Import Procedure Name Table
        /// </summary>
        public int e32_impproc => chunk.PeekInt32(OffsetOfImportProcedureNameTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Per-Page Checksum Table
        /// </summary>
        public int e32_pagesum => chunk.PeekInt32(OffsetOfPerPageChecksumTableOffset); //Relative to LE header

        /// <summary>
        /// Offset of Enumerated Data Pages
        /// </summary>
        public int e32_datapage => chunk.PeekInt32(OffsetOfEnumeratedDataPagesOffset); //Relative to beginning of file

        /// <summary>
        /// Number of preload pages
        /// </summary>
        public int e32_preload => chunk.PeekInt32(NumPreloadPagesOffset);

        /// <summary>
        /// Offset of Non-resident Names Table
        /// </summary>
        public int e32_nrestab => chunk.PeekInt32(OffsetOfNonResidentNamesTableOffset); //Relative to beginning of file

        /// <summary>
        /// Size of Non-resident Name Table
        /// </summary>
        public int e32_cbnrestab => chunk.PeekInt32(SizeOfNonResidentNameTableOffset);

        /// <summary>
        /// Non-resident Name Table Checksum
        /// </summary>
        public int e32_nressum => chunk.PeekInt32(NonResidentNameTableChecksumOffset); //Relative to LE header

        /// <summary>
        /// Object # for automatic data object
        /// </summary>
        public int e32_autodata => chunk.PeekInt32(ObjectNumForAutomaticDataObjectOffset);

        /// <summary>
        /// Offset of the debugging information
        /// </summary>
        public int e32_debuginfo => chunk.PeekInt32(OffsetOfDebugInfoOffset); //Relative to LE header

        /// <summary>
        /// The length of the debugging info. in bytes
        /// </summary>
        public int e32_debuglen => chunk.PeekInt32(DebugInfoLengthOffset);

        /// <summary>
        /// Number of instance pages in preload section of VXD file
        /// </summary>
        public int e32_instpreload => chunk.PeekInt32(NumPreloadSectionInstancePagesOffset);

        /// <summary>
        /// Number of instance pages in demand load section of VXD file
        /// </summary>
        public int e32_instdemand => chunk.PeekInt32(NumDemandLoadInstancePagesOffset);

        /// <summary>
        /// Size of heap - for 16-bit apps
        /// </summary>
        public int e32_heapsize => chunk.PeekInt32(HeapSizeOffset);

        /// <summary>
        /// Reserved words
        /// </summary>
        public NativeSpan<byte> e32_res3 => chunk.PeekNativeSpan<byte>(ReservedOffset, 12);

        public int e32_winresoff => chunk.PeekInt32(e32_winresoffOffset);

        public int e32_winreslen => chunk.PeekInt32(e32_winreslenOffset);

        /// <summary>
        /// Device ID for VxD
        /// </summary>
        public ushort e32_devid => chunk.PeekUInt16(DeviceIDOffset);

        /// <summary>
        /// DDK version for VxD
        /// </summary>
        public ushort e32_ddkver => chunk.PeekUInt16(DDKVersionOffset);

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
            writer.NewStruct(this, ViewKind.ImageVXDHeader, StructSize);

        int IViewable.NumChildren() => 51;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(e32_magic), MagicOffset, e32_magic);
                    break;

                case 1:
                    structWriter.WriteField(nameof(e32_border), ByteOrderOffset, e32_border, sizeof(byte));
                    break;

                case 2:
                    structWriter.WriteField(nameof(e32_worder), WordOrderOffset, e32_worder, sizeof(byte));
                    break;

                case 3:
                    structWriter.WriteField(nameof(e32_level), ExeFormatLevelOffset, e32_level, sizeof(int));
                    break;

                case 4:
                    structWriter.WriteField(nameof(e32_cpu), CpuTypeOffset, e32_cpu, sizeof(short));
                    break;

                case 5:
                    structWriter.WriteField(nameof(e32_os), OSTypeOffset, e32_os, sizeof(ushort));
                    break;

                case 6:
                    structWriter.WriteField(nameof(e32_ver), ModuleVersionOffset, e32_ver);
                    break;

                case 7:
                    structWriter.WriteField(nameof(e32_mflags), ModuleFlagsOffset, e32_mflags, sizeof(int));
                    break;

                case 8:
                    structWriter.WriteField(nameof(e32_mpages), NumModulePagesOffset, e32_mpages);
                    break;

                case 9:
                    structWriter.WriteField(nameof(e32_startobj), ObjectNumForIPOffset, e32_startobj);
                    break;

                case 10:
                    structWriter.WriteField(nameof(e32_eip), EIPOffset, e32_eip);
                    break;

                case 11:
                    structWriter.WriteField(nameof(e32_stackobj), ObjectNumForSPOffset, e32_stackobj);
                    break;

                case 12:
                    structWriter.WriteField(nameof(e32_esp), ESPOffset, e32_esp);
                    break;

                case 13:
                    structWriter.WriteField(nameof(e32_pagesize), PageSizeOffset, e32_pagesize);
                    break;

                case 14:
                    structWriter.WriteField(nameof(e32_lastpagesize), LastPageSizeOffset, e32_lastpagesize);
                    break;

                case 15:
                    structWriter.WriteField(nameof(e32_fixupsize), FixupSectionSizeOffset, e32_fixupsize);
                    break;

                case 16:
                    structWriter.WriteField(nameof(e32_fixupsum), FixupSectionChecksumOffset, e32_fixupsum);
                    break;

                case 17:
                    structWriter.WriteField(nameof(e32_ldrsize), LoaderSectionSizeOffset, e32_ldrsize);
                    break;

                case 18:
                    structWriter.WriteField(nameof(e32_ldrsum), LoaderSectionChecksumOffset, e32_ldrsum);
                    break;

                case 19:
                    structWriter.WriteField(nameof(e32_objtab), OffsetOfObjectTableOffset, e32_objtab);
                    break;

                case 20:
                    structWriter.WriteField(nameof(e32_objcnt), NumObjectsInModuleOffset, e32_objcnt);
                    break;

                case 21:
                    structWriter.WriteField(nameof(e32_objmap), OffsetOfObjectPageMapOffset, e32_objmap);
                    break;

                case 22:
                    structWriter.WriteField(nameof(e32_itermap), OffsetOfIteratedDataMapOffset, e32_itermap);
                    break;

                case 23:
                    structWriter.WriteField(nameof(e32_rsrctab), OffsetOfResourceTableOffset, e32_rsrctab);
                    break;

                case 24:
                    structWriter.WriteField(nameof(e32_rsrccnt), NumResourceEntriesOffset, e32_rsrccnt);
                    break;

                case 25:
                    structWriter.WriteField(nameof(e32_restab), OffsetOfResidentNameTableOffset, e32_restab);
                    break;

                case 26:
                    structWriter.WriteField(nameof(e32_enttab), OffsetOfEntryTableOffset, e32_enttab);
                    break;

                case 27:
                    structWriter.WriteField(nameof(e32_dirtab), OffsetOfModuleDirectiveTableOffset, e32_dirtab);
                    break;

                case 28:
                    structWriter.WriteField(nameof(e32_dircnt), NumModuleDirectivesOffset, e32_dircnt);
                    break;

                case 29:
                    structWriter.WriteField(nameof(e32_fpagetab), OffsetOfFixupPageTableOffset, e32_fpagetab);
                    break;

                case 30:
                    structWriter.WriteField(nameof(e32_frectab), OffsetOfFixupRecordTableOffset, e32_frectab);
                    break;

                case 31:
                    structWriter.WriteField(nameof(e32_impmod), OffsetOfImportModuleNameTableOffset, e32_impmod);
                    break;

                case 32:
                    structWriter.WriteField(nameof(e32_impmodcnt), NumImportModuleNameTableEntriesOffset, e32_impmodcnt);
                    break;

                case 33:
                    structWriter.WriteField(nameof(e32_impproc), OffsetOfImportProcedureNameTableOffset, e32_impproc);
                    break;

                case 34:
                    structWriter.WriteField(nameof(e32_pagesum), OffsetOfPerPageChecksumTableOffset, e32_pagesum);
                    break;

                case 35:
                    structWriter.WriteField(nameof(e32_datapage), OffsetOfEnumeratedDataPagesOffset, e32_datapage);
                    break;

                case 36:
                    structWriter.WriteField(nameof(e32_preload), NumPreloadPagesOffset, e32_preload);
                    break;

                case 37:
                    structWriter.WriteField(nameof(e32_nrestab), OffsetOfNonResidentNamesTableOffset, e32_nrestab);
                    break;

                case 38:
                    structWriter.WriteField(nameof(e32_cbnrestab), SizeOfNonResidentNameTableOffset, e32_cbnrestab);
                    break;

                case 39:
                    structWriter.WriteField(nameof(e32_nressum), NonResidentNameTableChecksumOffset, e32_nressum);
                    break;

                case 40:
                    structWriter.WriteField(nameof(e32_autodata), ObjectNumForAutomaticDataObjectOffset, e32_autodata);
                    break;

                case 41:
                    structWriter.WriteField(nameof(e32_debuginfo), OffsetOfDebugInfoOffset, e32_debuginfo);
                    break;

                case 42:
                    structWriter.WriteField(nameof(e32_debuglen), DebugInfoLengthOffset, e32_debuglen);
                    break;

                case 43:
                    structWriter.WriteField(nameof(e32_instpreload), NumPreloadSectionInstancePagesOffset, e32_instpreload);
                    break;

                case 44:
                    structWriter.WriteField(nameof(e32_instdemand), NumDemandLoadInstancePagesOffset, e32_instdemand);
                    break;

                case 45:
                    structWriter.WriteField(nameof(e32_heapsize), HeapSizeOffset, e32_heapsize);
                    break;

                case 46:
                    structWriter.WriteField(nameof(e32_res3), ReservedOffset, e32_res3);
                    break;

                case 47:
                    structWriter.WriteField(nameof(e32_winresoff), e32_winresoffOffset, e32_winresoff);
                    break;

                case 48:
                    structWriter.WriteField(nameof(e32_winreslen), e32_winreslenOffset, e32_winreslen);
                    break;

                case 49:
                    structWriter.WriteField(nameof(e32_devid), DeviceIDOffset, e32_devid);
                    break;

                case 50:
                    structWriter.WriteField(nameof(e32_ddkver), DDKVersionOffset, e32_ddkver);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
