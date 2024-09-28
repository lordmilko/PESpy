using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_DYNAMIC_RELOCATION_* enumeration which describes the type of value contained
    /// in a <see cref="IMAGE_DYNAMIC_RELOCATION32"/> / <see cref="IMAGE_DYNAMIC_RELOCATION64"/> structure.
    /// </summary>
    public enum ImageDynamicRelocationKind : long
    {
        GUARD_RF_PROLOGUE = 0x00000001,
        GUARD_RF_EPILOGUE = 0x00000002,

        /// <summary>
        /// IMAGE_DYNAMIC_RELOCATION_GUARD_IMPORT_CONTROL_TRANSFER<para/>
        /// Specifies that the value is an array of <see cref="IMAGE_BASE_RELOCATION"/> entries whose Type/Offset is an array of <see cref="IMAGE_IMPORT_CONTROL_TRANSFER_DYNAMIC_RELOCATION"/>.
        /// </summary>
        GUARD_IMPORT_CONTROL_TRANSFER = 0x00000003,

        /// <summary>
        /// IMAGE_DYNAMIC_RELOCATION_GUARD_INDIR_CONTROL_TRANSFER<para/>
        /// Specifies that the value is an array of <see cref="IMAGE_BASE_RELOCATION"/> entries whose Type/Offset is an array of <see cref="IMAGE_INDIR_CONTROL_TRANSFER_DYNAMIC_RELOCATION"/>.
        /// </summary>
        GUARD_INDIR_CONTROL_TRANSFER = 0x00000004,

        /// <summary>
        /// IMAGE_DYNAMIC_RELOCATION_GUARD_SWITCHTABLE_BRANCH<para/>
        /// Specifies that the value is an array of  <see cref="IMAGE_BASE_RELOCATION"/> entries whose Type/Offset is an array of <see cref="IMAGE_SWITCHTABLE_BRANCH_DYNAMIC_RELOCATION"/>.
        /// </summary>
        GUARD_SWITCHTABLE_BRANCH = 0x00000005,

        /// <summary>
        /// IMAGE_DYNAMIC_RELOCATION_FUNCTION_OVERRIDE<para/>
        /// Specifies that the value is a <see cref="IMAGE_FUNCTION_OVERRIDE_HEADER"/>.
        /// </summary>
        FUNCTION_OVERRIDE = 0x00000007
    }
}
