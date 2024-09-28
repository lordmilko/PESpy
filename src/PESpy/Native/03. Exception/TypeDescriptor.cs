using System;

namespace PESpy.Native
{
    internal unsafe struct TypeDescriptor
    {
        public IntPtr pVFTable;
        public IntPtr spare;
        public fixed byte name[1];
    }
}
