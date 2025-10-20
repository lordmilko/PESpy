namespace PESpy.View
{
    public enum ViewByteKind
    {
        /// <summary>
        /// The meaning of the byte has not yet been established
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The byte is the start of an assembly instruction
        /// </summary>
        Code = 1,

        /// <summary>
        /// The byte is the start of a piece of data
        /// </summary>
        Data = 2,

        /// <summary>
        /// The byte is part of the body of an earlier <see cref="Code"/> or <see cref="Data"/> byte.
        /// </summary>
        Body = 3,
    }
}
