namespace PESpy.Native
{
    public unsafe struct IMAGE_VXD_HEADER
    {
        public ushort e32_magic;                   // Magic number
        public byte e32_border;                  // The byte ordering for the VXD
        public byte e32_worder;                  // The word ordering for the VXD
        public int e32_level;                   // The EXE format level for now = 0
        public ushort e32_cpu;                     // The CPU type
        public ushort e32_os;                      // The OS type
        public int e32_ver;                     // Module version
        public int e32_mflags;                  // Module flags
        public int e32_mpages;                  // Module # pages
        public int e32_startobj;                // Object # for instruction pointer
        public int e32_eip;                     // Extended instruction pointer
        public int e32_stackobj;                // Object # for stack pointer
        public int e32_esp;                     // Extended stack pointer
        public int e32_pagesize;                // VXD page size
        public int e32_lastpagesize;            // Last page size in VXD
        public int e32_fixupsize;               // Fixup section size
        public int e32_fixupsum;                // Fixup section checksum
        public int e32_ldrsize;                 // Loader section size
        public int e32_ldrsum;                  // Loader section checksum
        public int e32_objtab;                  // Object table offset
        public int e32_objcnt;                  // Number of objects in module
        public int e32_objmap;                  // Object page map offset
        public int e32_itermap;                 // Object iterated data map offset
        public int e32_rsrctab;                 // Offset of Resource Table
        public int e32_rsrccnt;                 // Number of resource entries
        public int e32_restab;                  // Offset of resident name table
        public int e32_enttab;                  // Offset of Entry Table
        public int e32_dirtab;                  // Offset of Module Directive Table
        public int e32_dircnt;                  // Number of module directives
        public int e32_fpagetab;                // Offset of Fixup Page Table
        public int e32_frectab;                 // Offset of Fixup Record Table
        public int e32_impmod;                  // Offset of Import Module Name Table
        public int e32_impmodcnt;               // Number of entries in Import Module Name Table
        public int e32_impproc;                 // Offset of Import Procedure Name Table
        public int e32_pagesum;                 // Offset of Per-Page Checksum Table
        public int e32_datapage;                // Offset of Enumerated Data Pages
        public int e32_preload;                 // Number of preload pages
        public int e32_nrestab;                 // Offset of Non-resident Names Table
        public int e32_cbnrestab;               // Size of Non-resident Name Table
        public int e32_nressum;                 // Non-resident Name Table Checksum
        public int e32_autodata;                // Object # for automatic data object
        public int e32_debuginfo;               // Offset of the debugging information
        public int e32_debuglen;                // The length of the debugging info. in bytes
        public int e32_instpreload;             // Number of instance pages in preload section of VXD file
        public int e32_instdemand;              // Number of instance pages in demand load section of VXD file
        public int e32_heapsize;                // Size of heap - for 16-bit apps
        public fixed byte e32_res3[12];                // Reserved words
        public int e32_winresoff;
        public int e32_winreslen;
        public ushort e32_devid;                   // Device ID for VxD
        public ushort e32_ddkver;                  // DDK version for VxD
    }
}
