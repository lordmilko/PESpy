namespace PESpy.Native
{
    internal unsafe struct IMAGE_ARCHIVE_MEMBER_HEADER
    {
        public const int IMAGE_ARCHIVE_START_SIZE = 8;
        public const string IMAGE_ARCHIVE_START = "!<arch>\n";
        public const string IMAGE_ARCHIVE_END = "`\n";
        public const string IMAGE_ARCHIVE_PAD = "\n"; //I'm not exactly sure what this is for, but MSDN clearly says that the names are padded with spaces
        public const string IMAGE_ARCHIVE_LINKER_MEMBER = "/               ";
        public const string IMAGE_ARCHIVE_LONGNAMES_MEMBER = "//              ";
        public const string IMAGE_ARCHIVE_HYBRIDMAP_MEMBER = "/<HYBRIDMAP>/   ";

        public fixed byte Name[16];     // File member name - `/' terminated.
        public fixed byte Date[12];     // File member date - decimal.
        public fixed byte UserID[6];    // File member user id - decimal.
        public fixed byte GroupID[6];   // File member group id - decimal.
        public fixed byte Mode[8];      // File member mode - octal.
        public fixed byte Size[10];     // File member size - decimal.
        public fixed byte EndHeader[2]; // String to end header.
    }
}
