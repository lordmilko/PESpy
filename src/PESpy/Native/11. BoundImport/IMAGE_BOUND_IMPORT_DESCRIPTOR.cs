using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageBoundImportDescriptor
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_BOUND_IMPORT_DESCRIPTOR
    {
        public int TimeDateStamp;
        public ushort OffsetModuleName;
        public ushort NumberOfModuleForwarderRefs;

        // Array of zero or more IMAGE_BOUND_FORWARDER_REF follows
    }
}