using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageResourceDirectory
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IMAGE_RESOURCE_DIRECTORY
    {
        public int Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort NumberOfNamedEntries;
        public ushort NumberOfIdEntries;

        //IMAGE_RESOURCE_DIRECTORY_ENTRY
        public fixed byte DirectoryEntries[1];
    }
}
