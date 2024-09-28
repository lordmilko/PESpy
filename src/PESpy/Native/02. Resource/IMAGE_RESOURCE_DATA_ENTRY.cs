using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageResourceDataEntry
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_RESOURCE_DATA_ENTRY
    {
        public int OffsetToData;
        public int Size;
        public int CodePage;
        public int Reserved;
    }
}