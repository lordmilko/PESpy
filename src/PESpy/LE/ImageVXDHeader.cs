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

        /// <summary>
        /// Magic number<para/>
        /// e32_magic
        /// </summary>
        public ushort Magic => chunk.PeekUInt16(0);

        /// <summary>
        /// The byte ordering for the VXD<para/>
        /// e32_border
        /// </summary>
        public E32ByteOrder ByteOrder => (E32ByteOrder) chunk.PeekByte(2);

        /// <summary>
        /// The word ordering for the VXD<para/>
        /// e32_worder
        /// </summary>
        public E32WordOrder WordOrder => (E32WordOrder) chunk.PeekByte(3);

        /// <summary>
        /// The EXE format level for now = 0<para/>
        /// e32_level
        /// </summary>
        public E32Level ExeFormatLevel => (E32Level) chunk.PeekUInt32(4);

        /// <summary>
        /// The CPU type<para/>
        /// e32_cpu
        /// </summary>
        public E32CPU CpuType => (E32CPU) chunk.PeekUInt16(8);

        /// <summary>
        /// The OS type<para/>
        /// e32_os
        /// </summary>
        public NewOperatingSystem OSType => (NewOperatingSystem) chunk.PeekUInt16(10);

        /// <summary>
        /// Module version<para/>
        /// e32_ver
        /// </summary>
        public int ModuleVersion => chunk.PeekInt32(12);

        /// <summary>
        /// Module flags<para/>
        /// e32_mflags
        /// </summary>
        public E32ModuleFlags ModuleFlags => (E32ModuleFlags) chunk.PeekUInt32(16);

        /// <summary>
        /// Module # pages<para/>
        /// e32_mpages
        /// </summary>
        public int NumModulePages => chunk.PeekInt32(20);

        /// <summary>
        /// Object # for instruction pointer<para/>
        /// e32_startobj
        /// </summary>
        public int ObjectNumForIP => chunk.PeekInt32(24);

        /// <summary>
        /// Extended instruction pointer<para/>
        /// e32_eip
        /// </summary>
        public int EIP => chunk.PeekInt32(28);

        /// <summary>
        /// Object # for stack pointer<para/>
        /// e32_stackobj
        /// </summary>
        public int ObjectNumForSP => chunk.PeekInt32(32);

        /// <summary>
        /// Extended stack pointer<para/>
        /// e32_esp
        /// </summary>
        public int ESP => chunk.PeekInt32(36);

        /// <summary>
        /// VXD page size<para/>
        /// e32_pagesize
        /// </summary>
        public int PageSize => chunk.PeekInt32(40);

        /// <summary>
        /// Last page size in VXD<para/>
        /// e32_lastpagesize
        /// </summary>
        public int LastPageSize => chunk.PeekInt32(44);

        /// <summary>
        /// Fixup section size<para/>
        /// e32_fixupsize
        /// </summary>
        public int FixupSectionSize => chunk.PeekInt32(48);

        /// <summary>
        /// Fixup section checksum<para/>
        /// e32_fixupsum
        /// </summary>
        public int FixupSectionChecksum => chunk.PeekInt32(52);

        /// <summary>
        /// Loader section size<para/>
        /// e32_ldrsize
        /// </summary>
        public int LoaderSectionSize => chunk.PeekInt32(56);

        /// <summary>
        /// Loader section checksum<para/>
        /// e32_ldrsum
        /// </summary>
        public int LoaderSectionChecksum => chunk.PeekInt32(60);

        /// <summary>
        /// Object table offset<para/>
        /// e32_objtab
        /// </summary>
        public int OffsetOfObjectTable => chunk.PeekInt32(64); //Relative to LE header

        /// <summary>
        /// Number of objects in module<para/>
        /// e32_objcnt
        /// </summary>
        public int NumObjectsInModule => chunk.PeekInt32(68);

        /// <summary>
        /// Object page map offset<para/>
        /// e32_objmap
        /// </summary>
        public int OffsetOfObjectPageMap => chunk.PeekInt32(72); //Relative to LE header

        /// <summary>
        /// Object iterated data map offset<para/>
        /// e32_itermap
        /// </summary>
        public int OffsetOfIteratedDataMap => chunk.PeekInt32(76); //Relative to beginning of file

        /// <summary>
        /// Offset of Resource Table<para/>
        /// e32_rsrctab
        /// </summary>
        public int OffsetOfResourceTable => chunk.PeekInt32(80); //Relative to LE header

        /// <summary>
        /// Number of resource entries<para/>
        /// e32_rsrccnt
        /// </summary>
        public int NumResourceEntries => chunk.PeekInt32(84);

        /// <summary>
        /// Offset of resident name table<para/>
        /// e32_restab
        /// </summary>
        public int OffsetOfResidentNameTable => chunk.PeekInt32(88); //Relative to LE header

        /// <summary>
        /// Offset of Entry Table<para/>
        /// e32_enttab
        /// </summary>
        public int OffsetOfEntryTable => chunk.PeekInt32(92); //Relative to LE header

        /// <summary>
        /// Offset of Module Directive Table<para/>
        /// e32_dirtab
        /// </summary>
        public int OffsetOfModuleDirectiveTable => chunk.PeekInt32(96); //Relative to LE header

        /// <summary>
        /// Number of module directives<para/>
        /// e32_dircnt
        /// </summary>
        public int NumModuleDirectives => chunk.PeekInt32(100);

        /// <summary>
        /// Offset of Fixup Page Table<para/>
        /// e32_fpagetab
        /// </summary>
        public int OffsetOfFixupPageTable => chunk.PeekInt32(104); //Relative to LE header

        /// <summary>
        /// Offset of Fixup Record Table<para/>
        /// e32_frectab
        /// </summary>
        public int OffsetOfFixupRecordTable => chunk.PeekInt32(108); //Relative to LE header

        /// <summary>
        /// Offset of Import Module Name Table<para/>
        /// e32_impmod
        /// </summary>
        public int OffsetOfImportModuleNameTable => chunk.PeekInt32(112); //Relative to LE header

        /// <summary>
        /// Number of entries in Import Module Name Table<para/>
        /// e32_impmodcnt
        /// </summary>
        public int NumImportModuleNameTableEntries => chunk.PeekInt32(116);

        /// <summary>
        /// Offset of Import Procedure Name Table<para/>
        /// e32_impproc
        /// </summary>
        public int OffsetOfImportProcedureNameTable => chunk.PeekInt32(120); //Relative to LE header

        /// <summary>
        /// Offset of Per-Page Checksum Table<para/>
        /// e32_pagesum
        /// </summary>
        public int OffsetOfPerPageChecksumTable => chunk.PeekInt32(124); //Relative to LE header

        /// <summary>
        /// Offset of Enumerated Data Pages<para/>
        /// e32_datapage
        /// </summary>
        public int OffsetOfEnumeratedDataPages => chunk.PeekInt32(128); //Relative to beginning of file

        /// <summary>
        /// Number of preload pages<para/>
        /// e32_preload
        /// </summary>
        public int NumPreloadPages => chunk.PeekInt32(132);

        /// <summary>
        /// Offset of Non-resident Names Table<para/>
        /// e32_nrestab
        /// </summary>
        public int OffsetOfNonResidentNamesTable => chunk.PeekInt32(136); //Relative to beginning of file

        /// <summary>
        /// Size of Non-resident Name Table<para/>
        /// e32_cbnrestab
        /// </summary>
        public int SizeOfNonResidentNameTable => chunk.PeekInt32(140);

        /// <summary>
        /// Non-resident Name Table Checksum<para/>
        /// e32_nressum
        /// </summary>
        public int NonResidentNameTableChecksum => chunk.PeekInt32(144); //Relative to LE header

        /// <summary>
        /// Object # for automatic data object<para/>
        /// e32_autodata
        /// </summary>
        public int ObjectNumForAutomaticDataObject => chunk.PeekInt32(148);

        /// <summary>
        /// Offset of the debugging information<para/>
        /// e32_debuginfo
        /// </summary>
        public int OffsetOfDebugInfo => chunk.PeekInt32(152); //Relative to LE header

        /// <summary>
        /// The length of the debugging info. in bytes<para/>
        /// e32_debuglen
        /// </summary>
        public int DebugInfoLength => chunk.PeekInt32(156);

        /// <summary>
        /// Number of instance pages in preload section of VXD file<para/>
        /// e32_instpreload
        /// </summary>
        public int NumPreloadSectionInstancePages => chunk.PeekInt32(160);

        /// <summary>
        /// Number of instance pages in demand load section of VXD file<para/>
        /// e32_instdemand
        /// </summary>
        public int NumDemandLoadInstancePages => chunk.PeekInt32(164);

        /// <summary>
        /// Size of heap - for 16-bit apps<para/>
        /// e32_heapsize
        /// </summary>
        public int HeapSize => chunk.PeekInt32(168);

        /// <summary>
        /// Reserved words<para/>
        /// e32_res3
        /// </summary>
        public NativeSpan<byte> Reserved => chunk.PeekNativeSpan<byte>(172, 12);

        public int e32_winresoff => chunk.PeekInt32(184);

        public int e32_winreslen => chunk.PeekInt32(188);

        /// <summary>
        /// Device ID for VxD<para/>
        /// e32_devid
        /// </summary>
        public ushort DeviceID => chunk.PeekUInt16(192);

        /// <summary>
        /// DDK version for VxD<para/>
        /// e32_ddkver
        /// </summary>
        public ushort DDKVersion => chunk.PeekUInt16(194);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("e32_magic", Magic);
            s.WriteField("e32_border", ByteOrder, sizeof(byte));
            s.WriteField("e32_worder", WordOrder, sizeof(byte));
            s.WriteField("e32_level", ExeFormatLevel, sizeof(int));
            s.WriteField("e32_cpu", CpuType, sizeof(short));
            s.WriteField("e32_os", OSType, sizeof(ushort));
            s.WriteField("e32_ver", ModuleVersion);
            s.WriteField("e32_mflags", ModuleFlags, sizeof(int));
            s.WriteField("e32_mpages", NumModulePages);
            s.WriteField("e32_startobj", ObjectNumForIP);
            s.WriteField("e32_eip", EIP);
            s.WriteField("e32_stackobj", ObjectNumForSP);
            s.WriteField("e32_esp", ESP);
            s.WriteField("e32_pagesize", PageSize);
            s.WriteField("e32_lastpagesize", LastPageSize);
            s.WriteField("e32_fixupsize", FixupSectionSize);
            s.WriteField("e32_fixupsum", FixupSectionChecksum);
            s.WriteField("e32_ldrsize", LoaderSectionSize);
            s.WriteField("e32_ldrsum", LoaderSectionChecksum);
            s.WriteField("e32_objtab", OffsetOfObjectTable);
            s.WriteField("e32_objcnt", NumObjectsInModule);
            s.WriteField("e32_objmap", OffsetOfObjectPageMap);
            s.WriteField("e32_itermap", OffsetOfIteratedDataMap);
            s.WriteField("e32_rsrctab", OffsetOfResourceTable);
            s.WriteField("e32_rsrccnt", NumResourceEntries);
            s.WriteField("e32_restab", OffsetOfResidentNameTable);
            s.WriteField("e32_enttab", OffsetOfEntryTable);
            s.WriteField("e32_dirtab", OffsetOfModuleDirectiveTable);
            s.WriteField("e32_dircnt", NumModuleDirectives);
            s.WriteField("e32_fpagetab", OffsetOfFixupPageTable);
            s.WriteField("e32_frectab", OffsetOfFixupRecordTable);
            s.WriteField("e32_impmod", OffsetOfImportModuleNameTable);
            s.WriteField("e32_impmodcnt", NumImportModuleNameTableEntries);
            s.WriteField("e32_impproc", OffsetOfImportProcedureNameTable);
            s.WriteField("e32_pagesum", OffsetOfPerPageChecksumTable);
            s.WriteField("e32_datapage", OffsetOfEnumeratedDataPages);
            s.WriteField("e32_preload", NumPreloadPages);
            s.WriteField("e32_nrestab", OffsetOfNonResidentNamesTable);
            s.WriteField("e32_cbnrestab", SizeOfNonResidentNameTable);
            s.WriteField("e32_nressum", NonResidentNameTableChecksum);
            s.WriteField("e32_autodata", ObjectNumForAutomaticDataObject);
            s.WriteField("e32_debuginfo", OffsetOfDebugInfo);
            s.WriteField("e32_debuglen", DebugInfoLength);
            s.WriteField("e32_instpreload", NumPreloadSectionInstancePages);
            s.WriteField("e32_instdemand", NumDemandLoadInstancePages);
            s.WriteField("e32_heapsize", HeapSize);
            s.WriteField("e32_res3", Reserved);
            s.WriteField("e32_winresoff", e32_winresoff);
            s.WriteField("e32_winreslen", e32_winreslen);
            s.WriteField("e32_devid", DeviceID);
            s.WriteField("e32_ddkver", DDKVersion);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
