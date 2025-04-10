using System;

namespace PESpy.Native
{
    //ClrDebugResource
    internal struct CLR_DEBUG_RESOURCE
    {
        public int dwVersion;
        public Guid signature;
        public int dwDacTimeStamp;
        public int dwDacSizeOfImage;
        public int dwDbiTimeStamp;
        public int dwDbiSizeOfImage;
    }
}
