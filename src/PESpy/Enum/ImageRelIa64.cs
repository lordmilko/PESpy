namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_IA64_* enumeration that describes IA64 relocation types.
    /// </summary>
    public enum ImageRelIa64 : short
    {
        /// <summary>
        /// IMAGE_REL_IA64_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// IMAGE_REL_IA64_IMM14
        /// </summary>
        Imm14 = 0x0001,

        /// <summary>
        /// IMAGE_REL_IA64_IMM22
        /// </summary>
        Imm22 = 0x0002,

        /// <summary>
        /// IMAGE_REL_IA64_IMM64
        /// </summary>
        Imm64 = 0x0003,

        /// <summary>
        /// IMAGE_REL_IA64_DIR32
        /// </summary>
        Dir32 = 0x0004,

        /// <summary>
        /// IMAGE_REL_IA64_DIR64
        /// </summary>
        Dir64 = 0x0005,

        /// <summary>
        /// IMAGE_REL_IA64_PCREL21B
        /// </summary>
        PCRel21B = 0x0006,

        /// <summary>
        /// IMAGE_REL_IA64_PCREL21M
        /// </summary>
        PCRel21M = 0x0007,

        /// <summary>
        /// IMAGE_REL_IA64_PCREL21F
        /// </summary>
        PCRel21F = 0x0008,

        /// <summary>
        /// IMAGE_REL_IA64_GPREL22
        /// </summary>
        GPRel22 = 0x0009,

        /// <summary>
        /// IMAGE_REL_IA64_LTOFF22
        /// </summary>
        LtOff22 = 0x000A,

        /// <summary>
        /// IMAGE_REL_IA64_SECTION
        /// </summary>
        Section = 0x000B,

        /// <summary>
        /// IMAGE_REL_IA64_SECREL22
        /// </summary>
        SecRel22 = 0x000C,

        /// <summary>
        /// IMAGE_REL_IA64_SECREL64I
        /// </summary>
        SecRel64I = 0x000D,

        /// <summary>
        /// IMAGE_REL_IA64_SECREL32
        /// </summary>
        SecRel32 = 0x000E,

        /// <summary>
        /// IMAGE_REL_IA64_DIR32NB
        /// </summary>
        Dir32NB = 0x0010,

        /// <summary>
        /// IMAGE_REL_IA64_SREL14
        /// </summary>
        SRel14 = 0x0011,

        /// <summary>
        /// IMAGE_REL_IA64_SREL22
        /// </summary>
        SREL22 = 0x0012,

        /// <summary>
        /// IMAGE_REL_IA64_SREL32
        /// </summary>
        SRel32 = 0x0013,

        /// <summary>
        /// IMAGE_REL_IA64_UREL32
        /// </summary>
        URel32 = 0x0014,

        /// <summary>
        /// This is always a BRL and never converted<para/>
        /// IMAGE_REL_IA64_PCREL60X
        /// </summary>
        PCRel60X = 0x0015,

        /// <summary>
        /// If possible, convert to MBB bundle with NOP.B in slot 1<para/>
        /// IMAGE_REL_IA64_PCREL60B
        /// </summary>
        PCRel60B = 0x0016,

        /// <summary>
        /// If possible, convert to MFB bundle with NOP.F in slot 1<para/>
        /// IMAGE_REL_IA64_PCREL60F
        /// </summary>
        PCRel60F = 0x0017,

        /// <summary>
        /// If possible, convert to MIB bundle with NOP.I in slot 1<para/>
        /// IMAGE_REL_IA64_PCREL60I
        /// </summary>
        PCRel60I = 0x0018,

        /// <summary>
        /// If possible, convert to MMB bundle with NOP.M in slot 1<para/>
        /// IMAGE_REL_IA64_PCREL60M
        /// </summary>
        PCRel60M = 0x0019,

        /// <summary>
        /// IMAGE_REL_IA64_IMMGPREL64
        /// </summary>
        ImmGPRel64 = 0x001A,

        /// <summary>
        /// clr token<para/>
        /// IMAGE_REL_IA64_TOKEN
        /// </summary>
        Token = 0x001B,

        /// <summary>
        /// IMAGE_REL_IA64_GPREL32
        /// </summary>
        GPRel32 = 0x001C,

        /// <summary>
        /// IMAGE_REL_IA64_ADDEND
        /// </summary>
        AddEnd = 0x001F
    }
}
