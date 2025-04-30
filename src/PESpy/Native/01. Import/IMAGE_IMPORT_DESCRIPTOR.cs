using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageImportDescriptor
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_IMPORT_DESCRIPTOR
    {
        public int OriginalFirstThunk; // RVA to original unbound IAT (PIMAGE_THUNK_DATA)
        public uint TimeDateStamp;      // 0 if not bound,
                                       // -1 if bound, and real date\time stamp
                                       //     in IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT (new BIND)
                                       // O.W. date/time stamp of DLL bound to (Old BIND)

        public int ForwarderChain;     // -1 if no forwarders
        public int Name;
        public int FirstThunk;         // RVA to IAT (if bound this IAT has actual addresses)
    }
}
