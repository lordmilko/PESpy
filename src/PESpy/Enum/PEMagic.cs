using System.ComponentModel;

namespace PESpy
{
    public enum PEMagic : ushort
    {
        [Description("IMAGE_NT_OPTIONAL_HDR32_MAGIC")]
        PE32 = 0x10B,

        [Description("IMAGE_NT_OPTIONAL_HDR64_MAGIC")]
        PE32Plus = 0x20B,

        [Description("IMAGE_ROM_OPTIONAL_HDR_MAGIC")]
        ROM = 0x107
    }
}
