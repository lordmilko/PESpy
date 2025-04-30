using System.Runtime.InteropServices;

namespace PESpy
{
    //ImageDelayLoadDescriptor
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_DELAYLOAD_DESCRIPTOR
    {
        public int Attributes;
        public int DllNameRVA;                       // RVA to the name of the target library (NULL-terminate ASCII string)
        public int ModuleHandleRVA;                  // RVA to the HMODULE caching location (PHMODULE)
        public int ImportAddressTableRVA;            // RVA to the start of the IAT (PIMAGE_THUNK_DATA)
        public int ImportNameTableRVA;               // RVA to the start of the name table (PIMAGE_THUNK_DATA::AddressOfData)
        public int BoundImportAddressTableRVA;       // RVA to an optional bound IAT
        public int UnloadInformationTableRVA;        // RVA to an optional unload info table
        public uint TimeDateStamp;                    // 0 if not bound, otherwise, date/time of the target DLL
    }
}