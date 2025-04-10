using System;

namespace PESpy.Native
{
    struct DebugTypeEntry
    {
        public IntPtr TypeName;
        public IntPtr FieldName;
        public int FieldOffset;
        public int ReservedPadding;
    }
}