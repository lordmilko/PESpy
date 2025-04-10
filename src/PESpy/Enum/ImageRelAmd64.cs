namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_AMD64_* enumeration that describes x64 relocation types.
    /// </summary>
    public enum ImageRelAmd64 : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary<para/>
        /// IMAGE_REL_AMD64_ABSOLUTE
        /// </summary>
        Absolute = 0x0000,

        /// <summary>
        /// 64-bit address (VA).<para/>
        /// IMAGE_REL_AMD64_ADDR64
        /// </summary>
        Addr64 = 0x0001,

        /// <summary>
        /// 32-bit address (VA).<para/>
        /// IMAGE_REL_AMD64_ADDR32
        /// </summary>
        Addr32 = 0x0002,

        /// <summary>
        /// 32-bit address w/o image base (RVA).<para/>
        /// IMAGE_REL_AMD64_ADDR32NB
        /// </summary>
        Addr32NB = 0x0003,

        /// <summary>
        /// 32-bit relative address from byte following reloc<para/>
        /// IMAGE_REL_AMD64_REL32
        /// </summary>
        Rel32 = 0x0004,

        /// <summary>
        /// 32-bit relative address from byte distance 1 from reloc<para/>
        /// IMAGE_REL_AMD64_REL32_1
        /// </summary>
        Rel32_1 = 0x0005,

        /// <summary>
        /// 32-bit relative address from byte distance 2 from reloc<para/>
        /// IMAGE_REL_AMD64_REL32_2
        /// </summary>
        Rel32_2 = 0x0006,

        /// <summary>
        /// 32-bit relative address from byte distance 3 from reloc<para/>
        /// IMAGE_REL_AMD64_REL32_3
        /// </summary>
        Rel32_3 = 0x0007,

        /// <summary>
        /// 32-bit relative address from byte distance 4 from reloc<para/>
        /// IMAGE_REL_AMD64_REL32_4
        /// </summary>
        Rel32_4 = 0x0008,

        /// <summary>
        /// 32-bit relative address from byte distance 5 from reloc<para/>
        /// IMAGE_REL_AMD64_REL32_5
        /// </summary>
        Rel32_5 = 0x0009,

        /// <summary>
        /// Section index<para/>
        /// IMAGE_REL_AMD64_SECTION
        /// </summary>
        Section = 0x000A,

        /// <summary>
        /// 32 bit offset from base of section containing target<para/>
        /// IMAGE_REL_AMD64_SECREL
        /// </summary>
        SecRel = 0x000B,

        /// <summary>
        /// 7 bit unsigned offset from base of section containing target<para/>
        /// IMAGE_REL_AMD64_SECREL7
        /// </summary>
        SecRel7 = 0x000C,

        /// <summary>
        /// 32 bit metadata token<para/>
        /// IMAGE_REL_AMD64_TOKEN
        /// </summary>
        Token = 0x000D,

        /// <summary>
        /// 32 bit signed span-dependent value emitted into object<para/>
        /// IMAGE_REL_AMD64_SREL32
        /// </summary>
        SRel32 = 0x000E,

        /// <summary>
        /// IMAGE_REL_AMD64_PAIR
        /// </summary>
        Pair = 0x000F,

        /// <summary>
        /// 32 bit signed span-dependent value applied at link time<para/>
        /// IMAGE_REL_AMD64_SSPAN32
        /// </summary>
        SSpan32 = 0x0010,

        /// <summary>
        /// IMAGE_REL_AMD64_EHANDLER
        /// </summary>
        EHandler = 0x0011,

        /// <summary>
        /// Indirect branch to an import<para/>
        /// IMAGE_REL_AMD64_IMPORT_BR
        /// </summary>
        ImportBR = 0x0012,

        /// <summary>
        /// Indirect call to an import<para/>
        /// IMAGE_REL_AMD64_IMPORT_CALL
        /// </summary>
        ImportCall = 0x0013,

        /// <summary>
        /// Indirect branch to a CFG check<para/>
        /// IMAGE_REL_AMD64_CFG_BR
        /// </summary>
        CfgBr = 0x0014,

        /// <summary>
        /// Indirect branch to a CFG check, with REX.W prefix<para/>
        /// IMAGE_REL_AMD64_CFG_BR_REX
        /// </summary>
        CfgBrRex = 0x0015,

        /// <summary>
        /// Indirect call to a CFG check<para/>
        /// IMAGE_REL_AMD64_CFG_CALL
        /// </summary>
        CfgCall = 0x0016,

        /// <summary>
        /// Indirect branch to a target in RAX (no CFG)<para/>
        /// IMAGE_REL_AMD64_INDIR_BR
        /// </summary>
        IndirBr = 0x0017,

        /// <summary>
        /// Indirect branch to a target in RAX, with REX.W prefix (no CFG)<para/>
        /// IMAGE_REL_AMD64_INDIR_BR_REX
        /// </summary>
        IndirBrRex = 0x0018,

        /// <summary>
        /// Indirect call to a target in RAX (no CFG)<para/>
        /// IMAGE_REL_AMD64_INDIR_CALL
        /// </summary>
        IndirCall = 0x0019,

        /// <summary>
        /// Indirect branch for a switch table using Reg 0 (RAX)<para/>
        /// IMAGE_REL_AMD64_INDIR_BR_SWITCHTABLE_FIRST
        /// </summary>
        IndirBrSwitchTableFirst = 0x0020,

        /// <summary>
        /// Indirect branch for a switch table using Reg 15 (R15)<para/>
        /// IMAGE_REL_AMD64_INDIR_BR_SWITCHTABLE_LAST
        /// </summary>
        IndirBrSwitchTableLast = 0x002F
    }
}
