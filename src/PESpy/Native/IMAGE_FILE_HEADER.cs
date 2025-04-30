using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageFileHeader
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_FILE_HEADER
    {
        public ushort Machine; //IMAGE_FILE_MACHINE
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public int PointerToSymbolTable;
        public int NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ImageFile Characteristics;
    }
}
