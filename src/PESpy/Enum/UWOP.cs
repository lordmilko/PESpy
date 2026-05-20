using PESpy.Native;

namespace PESpy
{
    //https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170

    /// <summary>
    /// Represents the UWOP_* enumeration that describes the unwind operation code of a <see cref="UNWIND_INFO"/>
    /// </summary>
    public enum UWOP : byte
    {
        /// <summary>
        /// Push a nonvolatile integer register, decrementing RSP by 8. The operation info is the number of the register.
        /// Because of the constraints on epilogs, UWOP_PUSH_NONVOL unwind codes must appear first in the prolog and correspondingly,
        /// last in the unwind code array. This relative ordering applies to all other unwind codes except <see cref="UWOP_PUSH_MACHFRAME"/>.
        /// </summary>
        UWOP_PUSH_NONVOL = 0,

        /// <summary>
        /// Allocate a large-sized area on the stack. There are two forms. If the operation info equals 0, then the size of the allocation divided by 8
        /// is recorded in the next slot, allowing an allocation up to 512K - 8. If the operation info equals 1, then the unscaled size of the allocation
        /// is recorded in the next two slots in little-endian format, allowing allocations up to 4GB - 8.
        /// </summary>
        UWOP_ALLOC_LARGE = 1,

        /// <summary>
        /// Allocate a small-sized area on the stack. The size of the allocation is the operation info field * 8 + 8, allowing allocations from 8 to 128 bytes.
        /// </summary>
        UWOP_ALLOC_SMALL = 2,

        /// <summary>
        /// Establish the frame pointer register by setting the register to some offset of the current RSP. The offset is equal to the Frame Register
        /// offset (scaled) field in the UNWIND_INFO * 16, allowing offsets from 0 to 240. The use of an offset permits establishing a frame pointer that
        /// points to the middle of the fixed stack allocation, helping code density by allowing more accesses to use short instruction forms. The
        /// operation info field is reserved and shouldn't be used.
        /// </summary>
        UWOP_SET_FPREG = 3,

        /// <summary>
        /// Save a nonvolatile integer register on the stack using a MOV instead of a PUSH. This code is primarily used for shrink-wrapping, where a
        /// nonvolatile register is saved to the stack in a position that was previously allocated. The operation info is the number of the register.
        /// The scaled-by-8 stack offset is recorded in the next unwind operation code slot, as described in the note above.
        /// </summary>
        UWOP_SAVE_NONVOL = 4,

        /// <summary>
        /// Save a nonvolatile integer register on the stack with a long offset, using a MOV instead of a PUSH. This code is primarily used for
        /// shrink-wrapping, where a nonvolatile register is saved to the stack in a position that was previously allocated. The operation info is the
        /// number of the register. The unscaled stack offset is recorded in the next two unwind operation code slots, as described in the note above.
        /// </summary>
        UWOP_SAVE_NONVOL_FAR = 5,

        UWOP_EPILOG = 6, //Present in Version 2
        UWOP_SPARE_CODE = 7,
        UWOP_SAVE_XMM128 = 8,

        /// <summary>
        /// Save all 128 bits of a nonvolatile XMM register on the stack. The operation info is the number of the register. The scaled-by-16 stack offset
        /// is recorded in the next slot.
        /// </summary>
        UWOP_SAVE_XMM128_FAR = 9,

        /// <summary>
        /// Push a machine frame. This unwind code is used to record the effect of a hardware interrupt or exception. There are two forms.
        /// For more info see https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170
        /// </summary>
        UWOP_PUSH_MACHFRAME = 10
    }
}
