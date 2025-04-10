using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageImportByName
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IMAGE_IMPORT_BY_NAME
    {
        public ushort Hint;
        public fixed byte Name[1];
    }
}
