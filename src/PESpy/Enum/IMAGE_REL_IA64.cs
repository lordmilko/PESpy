namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_IA64_* enumeration that describes IA64 relocation types.
    /// </summary>
    public enum IMAGE_REL_IA64 : short
    {
        IMAGE_REL_IA64_ABSOLUTE = 0x0000,
        IMAGE_REL_IA64_IMM14 = 0x0001,
        IMAGE_REL_IA64_IMM22 = 0x0002,
        IMAGE_REL_IA64_IMM64 = 0x0003,
        IMAGE_REL_IA64_DIR32 = 0x0004,
        IMAGE_REL_IA64_DIR64 = 0x0005,
        IMAGE_REL_IA64_PCREL21B = 0x0006,
        IMAGE_REL_IA64_PCREL21M = 0x0007,
        IMAGE_REL_IA64_PCREL21F = 0x0008,
        IMAGE_REL_IA64_GPREL22 = 0x0009,
        IMAGE_REL_IA64_LTOFF22 = 0x000A,
        IMAGE_REL_IA64_SECTION = 0x000B,
        IMAGE_REL_IA64_SECREL22 = 0x000C,
        IMAGE_REL_IA64_SECREL64I = 0x000D,
        IMAGE_REL_IA64_SECREL32 = 0x000E,
        IMAGE_REL_IA64_DIR32NB = 0x0010,
        IMAGE_REL_IA64_SREL14 = 0x0011,
        IMAGE_REL_IA64_SREL22 = 0x0012,
        IMAGE_REL_IA64_SREL32 = 0x0013,
        IMAGE_REL_IA64_UREL32 = 0x0014,

        /// <summary>
        /// This is always a BRL and never converted
        /// </summary>
        IMAGE_REL_IA64_PCREL60X = 0x0015,

        /// <summary>
        /// If possible, convert to MBB bundle with NOP.B in slot 1
        /// </summary>
        IMAGE_REL_IA64_PCREL60B = 0x0016,

        /// <summary>
        /// If possible, convert to MFB bundle with NOP.F in slot 1
        /// </summary>
        IMAGE_REL_IA64_PCREL60F = 0x0017,

        /// <summary>
        /// If possible, convert to MIB bundle with NOP.I in slot 1
        /// </summary>
        v = 0x0018,

        /// <summary>
        /// If possible, convert to MMB bundle with NOP.M in slot 1
        /// </summary>
        IMAGE_REL_IA64_PCREL60M = 0x0019,

        IMAGE_REL_IA64_IMMGPREL64 = 0x001A,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_IA64_TOKEN = 0x001B,

        IMAGE_REL_IA64_GPREL32 = 0x001C,
        IMAGE_REL_IA64_ADDEND = 0x001F
    }
}
