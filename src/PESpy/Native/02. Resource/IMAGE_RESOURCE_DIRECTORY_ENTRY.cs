using System.Runtime.InteropServices;

namespace PESpy.Native
{
    //ImageResourceDirectoryEntry
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_RESOURCE_DIRECTORY_ENTRY
    {
        /*union {
            struct {
                ULONG NameOffset:31;
                ULONG NameIsString:1;
            };
            ULONG Name;
            USHORT Id;
        };*/
        public int NameOrId;

        /*union
        {
            ULONG OffsetToData;
            struct {
                ULONG OffsetToDirectory:31;
                ULONG DataIsDirectory:1;
            };
        };*/
        public int OffsetToData;
    }
}