using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_BASED_* enumeration that specifies the base relocation type of an <see cref="IMAGE_BASE_RELOCATION"/>.
    /// </summary>
    public enum IMAGE_REL_BASED : byte
    {
        /// <summary>
        /// The base relocation is skipped. This type can be used to pad a block.
        /// </summary>
        IMAGE_REL_BASED_ABSOLUTE = 0,

        /// <summary>
        /// The base relocation adds the high 16 bits of the difference to the 16-bit field at offset. The 16-bit field represents the high value of a 32-bit word.
        /// </summary>
        IMAGE_REL_BASED_HIGH = 1,

        /// <summary>
        /// The base relocation adds the low 16 bits of the difference to the 16-bit field at offset. The 16-bit field represents the low half of a 32-bit word.
        /// </summary>
        IMAGE_REL_BASED_LOW = 2,

        /// <summary>
        /// The base relocation applies all 32 bits of the difference to the 32-bit field at offset.
        /// </summary>
        IMAGE_REL_BASED_HIGHLOW = 3,

        /// <summary>
        /// The base relocation adds the high 16 bits of the difference to the 16-bit field at offset. The 16-bit field represents the high value of
        /// a 32-bit word. The low 16 bits of the 32-bit value are stored in the 16-bit word that follows this base relocation. This means that this
        /// base relocation occupies two slots.
        /// </summary>
        IMAGE_REL_BASED_HIGHADJ = 4,

        //Machine Specific

        /// <summary>
        /// The relocation interpretation is dependent on the machine type. When the machine type is MIPS, the base relocation applies to a MIPS jump instruction.
        /// </summary>
        IMAGE_REL_BASED_MIPS_JMPADDR = 5,

        /// <summary>
        /// This relocation is meaningful only when the machine type is ARM or Thumb. The base relocation applies the 32-bit address of a symbol across a consecutive MOVW/MOVT instruction pair.
        /// </summary>
        IMAGE_REL_BASED_ARM_MOV32 = 5,

        IMAGE_REL_BASED_RESERVED = 6,

        //Machine Specific

        /// <summary>
        /// This relocation is meaningful only when the machine type is Thumb. The base relocation applies the 32-bit address of a symbol to a consecutive MOVW/MOVT instruction pair.
        /// </summary>
        IMAGE_REL_BASED_THUMB_MOV32 = 7,

        //Machine Specific

        /// <summary>
        /// RISCV: This relocation is only meaningful when the machine type is RISC-V. The base relocation applies to the low 12 bits of a 32-bit absolute address formed in RISC-V S-type instruction format.<para/>
        /// LOONGARCH32: This relocation is only meaningful when the machine type is LoongArch 32-bit. The base relocation applies to a 32-bit absolute address formed in two consecutive instructions.<para/>
        /// LOONGARCH64: This relocation is only meaningful when the machine type is LoongArch 64-bit. The base relocation applies to a 64-bit absolute address formed in four consecutive instructions.
        /// </summary>
        IMAGE_REL_BASED_MACHINE_SPECIFIC_8 = 8,

        //Machine Specific

        IMAGE_REL_BASED_IA64_IMM64 = 9,

        /// <summary>
        /// The relocation is only meaningful when the machine type is MIPS. The base relocation applies to a MIPS16 jump instruction.
        /// </summary>
        IMAGE_REL_BASED_MIPS_JMPADDR16 = 9,

        /// <summary>
        /// The base relocation applies the difference to the 64-bit field at offset.
        /// </summary>
        IMAGE_REL_BASED_DIR64 = 10
    }
}
