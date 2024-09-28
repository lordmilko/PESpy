#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a value that was read from a file.
    /// </summary>
    public interface IValue
    {
        /// <summary>
        /// Gets the offset of this region within its parent file.
        /// </summary>
        /// <remarks>
        /// RVA references in PE files are 32-bit unsigned integers; therefore, the maximum
        /// file size of a PE is 4gb (otherwise you could have an RVA trying to reference an address
        /// above 4gb).
        /// </remarks>
        RawOffset Offset { get; }
    }
}
