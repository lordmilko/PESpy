namespace PESpy.LE
{
    public enum E32BundleType
    {
        /// <summary>
        /// Empty bundle
        /// </summary>
        EMPTY = 0x00,

        /// <summary>
        /// 16-bit offset entry point
        /// </summary>
        ENTRY16 = 0x01,

        /// <summary>
        /// 286 call gate (16-bit IOPL)
        /// </summary>
        GATE16 = 0x02,

        /// <summary>
        /// 32-bit offset entry point
        /// </summary>
        ENTRY32 = 0x03,

        /// <summary>
        /// Forwarder entry point
        /// </summary>
        ENTRYFWD = 0x04,

        /// <summary>
        /// Typing information present flag
        /// </summary>
        TYPEINFO = 0x80,
    }
}
