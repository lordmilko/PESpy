namespace PESpy.Native
{
    //https://github.com/dotnet/runtime/blob/main/src/native/corehost/bundle/file_entry.h
    internal struct file_entry_fixed_t
    {
        public long offset;
        public long size;
        public long compressedSize; //v6+ only
        public Bundle.file_type_t type;

        //Relative Path: 7 bit string length + UTF 8 encoded string
    }
}
