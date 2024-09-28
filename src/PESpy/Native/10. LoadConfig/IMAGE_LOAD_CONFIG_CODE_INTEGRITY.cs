using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageLoadConfigCodeIntegrity
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_LOAD_CONFIG_CODE_INTEGRITY
    {
        public ushort Flags; //Flags
        public ushort Catalog;
        public int CatalogOffset;
        public int Reserved;
    }
}