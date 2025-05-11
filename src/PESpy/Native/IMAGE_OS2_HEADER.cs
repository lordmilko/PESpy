namespace PESpy.Native
{
    //Also called new_exe
    public struct IMAGE_OS2_HEADER
    {
        public ushort ne_magic;                    // Magic number
        public byte ne_ver;                      // Version number
        public byte ne_rev;                      // Revision number
        public ushort ne_enttab;                   // Offset of Entry Table
        public ushort ne_cbenttab;                 // Number of bytes in Entry Table
        public int ne_crc;                      // Checksum of whole file
        public ushort ne_flags;                    // Flag word
        public ushort ne_autodata;                 // Automatic data segment number
        public ushort ne_heap;                     // Initial heap allocation
        public ushort ne_stack;                    // Initial stack allocation
        public int ne_csip;                     // Initial CS:IP setting
        public int ne_sssp;                     // Initial SS:SP setting
        public ushort ne_cseg;                     // Count of file segments
        public ushort ne_cmod;                     // Entries in Module Reference Table
        public ushort ne_cbnrestab;                // Size of non-resident name table
        public ushort ne_segtab;                   // Offset of Segment Table
        public ushort ne_rsrctab;                  // Offset of Resource Table
        public ushort ne_restab;                   // Offset of resident name table
        public ushort ne_modtab;                   // Offset of Module Reference Table
        public ushort ne_imptab;                   // Offset of Imported Names Table
        public int ne_nrestab;                  // Offset of Non-resident Names Table
        public ushort ne_cmovent;                  // Count of movable entries
        public ushort ne_align;                    // Segment alignment shift count
        public ushort ne_cres;                     // Count of resource segments
        public byte ne_exetyp;                   // Target Operating system
        public byte ne_flagsothers;              // Other .EXE flags

        //It seems like the next two fields could either be ne_gangstart and ne_ganglength or ne_pretthunks
        //and ne_psegrefbytes. It seems like the gang load fields are defined as pointers / aliases for these two fields.
        //So depending on the scenario they may be interpreted as containing gang load data

        public ushort ne_pretthunks;               // offset to return thunks
        public ushort ne_psegrefbytes;             // offset to segment ref. bytes

        public ushort ne_swaparea;                 // Minimum code swap area size
        public ushort ne_expver;                   // Expected Windows version number
    }
}
