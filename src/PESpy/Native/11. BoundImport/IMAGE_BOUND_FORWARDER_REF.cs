using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageBoundForwarderRef
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_BOUND_FORWARDER_REF
    {
        public uint TimeDateStamp;
        public ushort OffsetModuleName;
        public ushort Reserved;
    }
}