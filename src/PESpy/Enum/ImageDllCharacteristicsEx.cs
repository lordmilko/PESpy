using System;

namespace PESpy
{
    [Flags]
    public enum ImageDllCharacteristicsEx
    {
        CET_COMPAT = 1,
        CET_COMPAT_STRICT_MODE = 2,
        CET_SET_CONTEXT_IP_VALIDATION_RELAXED_MODE = 4,
        CET_DYNAMIC_APIS_ALLOW_IN_PROC = 8,
        CET_RESERVED_1 = 16,
        CET_RESERVED_2 = 32
    }
}