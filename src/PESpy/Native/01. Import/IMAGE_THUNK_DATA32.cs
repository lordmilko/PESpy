using System.Runtime.InteropServices;

namespace PESpy.Native
{
    /* From https://learn.microsoft.com/en-us/archive/msdn-magazine/2002/march/inside-windows-an-in-depth-look-into-the-win32-portable-executable-file-format-part-2
     *
     *     The IMAGE_THUNK_DATA structures within the IAT lead a dual-purpose life. In the executable file, they contain either the ordinal of the imported API or an RVA to an IMAGE_IMPORT_BY_NAME structure.
     *     The IMAGE_IMPORT_BY_NAME structure is just a WORD, followed by a string naming the imported API. The WORD value is a "hint" to the loader as to what the ordinal of the imported API might be.
     *     When the loader brings in the executable, it overwrites each IAT entry with the actual address of the imported function. This a key point to understand before proceeding. I highly recommend reading
     *     Russell Osterlund's article in this issue which describes the steps that the Windows loader takes.
     *
     *     Before the executable is loaded, is there a way you can tell if an IMAGE_THUNK_DATA structure contains an import ordinal, as opposed to an RVA to an IMAGE_IMPORT_BY_NAME structure? The key is the high bit
     *     of the IMAGE_THUNK_DATA value. If set, the bottom 31 bits (or 63 bits for a 64-bit executable) is treated as an ordinal value. If the high bit isn't set, the IMAGE_THUNK_ DATA value is an RVA to the
     *     IMAGE_IMPORT_BY_NAME.
     *
     *     The other array, the INT, is essentially identical to the IAT. It's also an array of IMAGE_THUNK_DATA structures. The key difference is that the INT isn't overwritten by the loader when brought into memory.
     *     Why have two parallel arrays for each set of APIs imported from a DLL? The answer is in a concept called binding. When the binding process rewrites the IAT in the file (I'll describe this process later),
     *     some way of getting the original information needs to remain. The INT, which is a duplicate copy of the information, is just the ticket.
     *
     *     An INT isn't required for an executable to load. However, if not present, the executable cannot be bound. The Microsoft linker seems to always emit an INT, but for a long time, the Borland linker (TLINK) did not.
     *     The Borland-created files could not be bound.
     */

    [StructLayout(LayoutKind.Explicit)]
    internal struct IMAGE_THUNK_DATA32
    {
        [FieldOffset(0)]
        public int ForwarderString;      // PUCHAR

        [FieldOffset(0)]
        public int Function;             // PULONG

        [FieldOffset(0)]
        public int Ordinal;

        [FieldOffset(0)]
        public int AddressOfData;        // PIMAGE_IMPORT_BY_NAME
    }
}
