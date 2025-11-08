namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_AMD64_* enumeration that describes x64 relocation types.
    /// </summary>
    public enum IMAGE_REL_AMD64 : short
    {
        /// <summary>
        /// Reference is absolute, no relocation is necessary
        /// </summary>
        IMAGE_REL_AMD64_ABSOLUTE = 0x0000,

        /// <summary>
        /// 64-bit address (VA).
        /// </summary>
        IMAGE_REL_AMD64_ADDR64 = 0x0001,

        /// <summary>
        /// 32-bit address (VA).
        /// </summary>
        IMAGE_REL_AMD64_ADDR32 = 0x0002,

        /// <summary>
        /// 32-bit address w/o image base (RVA).
        /// </summary>
        IMAGE_REL_AMD64_ADDR32NB = 0x0003,

        /// <summary>
        /// 32-bit relative address from byte following reloc
        /// </summary>
        IMAGE_REL_AMD64_REL32 = 0x0004,

        /// <summary>
        /// 32-bit relative address from byte distance 1 from reloc
        /// </summary>
        IMAGE_REL_AMD64_REL32_1 = 0x0005,

        /// <summary>
        /// 32-bit relative address from byte distance 2 from reloc
        /// </summary>
        IMAGE_REL_AMD64_REL32_2 = 0x0006,

        /// <summary>
        /// 32-bit relative address from byte distance 3 from reloc
        /// </summary>
        IMAGE_REL_AMD64_REL32_3 = 0x0007,

        /// <summary>
        /// 32-bit relative address from byte distance 4 from reloc
        /// </summary>
        IMAGE_REL_AMD64_REL32_4 = 0x0008,

        /// <summary>
        /// 32-bit relative address from byte distance 5 from reloc
        /// </summary>
        IMAGE_REL_AMD64_REL32_5 = 0x0009,

        /// <summary>
        /// Section index
        /// </summary>
        IMAGE_REL_AMD64_SECTION = 0x000A,

        /// <summary>
        /// 32 bit offset from base of section containing target
        /// </summary>
        IMAGE_REL_AMD64_SECREL = 0x000B,

        /// <summary>
        /// 7 bit unsigned offset from base of section containing target
        /// </summary>
        IMAGE_REL_AMD64_SECREL7 = 0x000C,

        /// <summary>
        /// 32 bit metadata token
        /// </summary>
        IMAGE_REL_AMD64_TOKEN = 0x000D,

        /// <summary>
        /// 32 bit signed span-dependent value emitted into object
        /// </summary>
        IMAGE_REL_AMD64_SREL32 = 0x000E,

        IMAGE_REL_AMD64_PAIR = 0x000F,

        /// <summary>
        /// 32 bit signed span-dependent value applied at link time
        /// </summary>
        IMAGE_REL_AMD64_SSPAN32 = 0x0010,

        IMAGE_REL_AMD64_EHANDLER = 0x0011,

        /// <summary>
        /// Indirect branch to an import
        /// </summary>
        IMAGE_REL_AMD64_IMPORT_BR = 0x0012,

        /// <summary>
        /// Indirect call to an import
        /// </summary>
        IMAGE_REL_AMD64_IMPORT_CALL = 0x0013,

        /// <summary>
        /// Indirect branch to a CFG check
        /// </summary>
        IMAGE_REL_AMD64_CFG_BR = 0x0014,

        /// <summary>
        /// Indirect branch to a CFG check, with REX.W prefix
        /// </summary>
        IMAGE_REL_AMD64_CFG_BR_REX = 0x0015,

        /// <summary>
        /// Indirect call to a CFG check
        /// </summary>
        IMAGE_REL_AMD64_CFG_CALL = 0x0016,

        /// <summary>
        /// Indirect branch to a target in RAX (no CFG)
        /// </summary>
        IMAGE_REL_AMD64_INDIR_BR = 0x0017,

        /// <summary>
        /// Indirect branch to a target in RAX, with REX.W prefix (no CFG)
        /// </summary>
        IMAGE_REL_AMD64_INDIR_BR_REX = 0x0018,

        /// <summary>
        /// Indirect call to a target in RAX (no CFG)
        /// </summary>
        IMAGE_REL_AMD64_INDIR_CALL = 0x0019,

        /// <summary>
        /// Indirect branch for a switch table using Reg 0 (RAX)
        /// </summary>
        IMAGE_REL_AMD64_INDIR_BR_SWITCHTABLE_FIRST = 0x0020,

        /// <summary>
        /// Indirect branch for a switch table using Reg 15 (R15)
        /// </summary>
        IMAGE_REL_AMD64_INDIR_BR_SWITCHTABLE_LAST = 0x002F
    }
}
