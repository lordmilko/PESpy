using System;

namespace PESpy
{
    [Flags]
    public enum CorCompileImportFlags : ushort
    {
        CORCOMPILE_IMPORT_FLAGS_EAGER = 0x0001,   // Section at module load time.
        CORCOMPILE_IMPORT_FLAGS_CODE = 0x0002,   // Section contains code.
        CORCOMPILE_IMPORT_FLAGS_PCODE = 0x0004,   // Section contains pointers to code.
    }
}
