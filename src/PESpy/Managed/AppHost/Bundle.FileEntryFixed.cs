namespace PESpy
{
    public static partial class Bundle
    {
        //file_entry_fixed_t
        public struct FileEntryFixed
        {
            public long Offset { get; }

            public long Size { get; }

            public long CompressedSize { get; }

            public file_type_t Type { get; }

            internal FileEntryFixed(IFileReader reader, int majorVersion)
            {
                Offset = reader.ReadInt64();
                Size = reader.ReadInt64();

                CompressedSize = majorVersion >= 6 ? reader.ReadInt64() : 0;

                Type = (file_type_t) reader.ReadByte();
            }
        }
    }    
}
