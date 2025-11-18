using System;

namespace PESpy
{
    [Flags]
    public enum CHD
    {
        CHD_MULTINH = 0x00000001,
        CHD_VIRTINH = 0x00000002,
        CHD_AMBIGUOUS = 0x00000004
    }
}
