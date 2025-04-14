namespace PESpy.Native
{
    //https://github.com/dotnet/runtime/blob/36ca267b3e501a1f49d89dc344380a77a76916c6/src/coreclr/nativeaot/Runtime/DebugHeader.cpp#L66
    internal unsafe struct DotNetRuntimeDebugHeader
    {
        public const int DebugTypeEntriesArraySize = 100;
        public const int GlobalEntriesArraySize = 8;

        //todo: cookie 0x44, 0x4E, 0x44, 0x48 //DNDH

        public short MajorVersion;
        public short MinorVersion;
        public int Flags;
        public int ReservedPadding1;

        public fixed byte DebugTypeEntries[1]; //100 * DebugTypeEntry. Actual length is variable; ends in a null terminator
        public fixed byte GlobalEntries[1]; //8 * GlobalValueEntry. Actual length is variable; ends in a null terminator
    }
}
