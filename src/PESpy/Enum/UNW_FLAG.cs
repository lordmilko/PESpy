using PESpy.Native;

namespace PESpy
{
    /// <summary>
    /// Represents the UNW_FLAG_* enumeration which describes the type of data contained at the end of a <see cref="UNWIND_INFO"/> structure.
    /// </summary>
    public enum UNW_FLAG
    {
        NHANDLER = 0,

        /// <summary>
        /// The function has an exception handler that should be called when looking for functions that need to examine exceptions.
        /// </summary>
        EHANDLER = 1,

        /// <summary>
        /// The function has a termination handler that should be called when unwinding an exception.
        /// </summary>
        UHANDLER = 2,

        FHANDLER = 3, //Reportedly unofficial

        /// <summary>
        /// This unwind info structure is not the primary one for the procedure. Instead, the chained unwind info entry is the contents of a previous
        /// RUNTIME_FUNCTION entry. For information, see Chained unwind info structures. If this flag is set, then the UNW_FLAG_EHANDLER and UNW_FLAG_UHANDLER
        /// flags must be cleared. Also, the frame register and fixed-stack allocation fields must have the same values as in the primary unwind info.
        /// </summary>
        CHAININFO = 4
    }
}
