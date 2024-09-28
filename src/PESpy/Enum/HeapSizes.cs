namespace PESpy
{
    public enum HeapSizes : byte
    {
        HEAP_STRING_4 = 0x01,
        HEAP_GUID_4 = 0x02,
        HEAP_BLOB_4 = 0x04,

        PADDING_BIT = 0x08,       // Tables can be created with an extra bit in columns, for growth.

        DELTA_ONLY = 0x20,       // If set, only deltas were persisted.
        EXTRA_DATA = 0x40,       // If set, schema persists an extra 4 bytes of data.
        HAS_DELETE = 0x80,       // If set, this metadata can contain _Delete tokens.
    }
}