namespace PESpy
{
    /// <summary>
    /// Represents the IMAGE_REL_M32R_* enumeration.
    /// </summary>
    public enum IMAGE_REL_M32R : short
    {
        /// <summary>
        /// No relocation required
        /// </summary>
        IMAGE_REL_M32R_ABSOLUTE = 0x0000,

        /// <summary>
        /// 32 bit address
        /// </summary>
        IMAGE_REL_M32R_ADDR32 = 0x0001,

        /// <summary>
        /// 32 bit address w/o image base
        /// </summary>
        IMAGE_REL_M32R_ADDR32NB = 0x0002,

        /// <summary>
        /// 24 bit address
        /// </summary>
        IMAGE_REL_M32R_ADDR24 = 0x0003,

        /// <summary>
        /// GP relative addressing
        /// </summary>
        IMAGE_REL_M32R_GPREL16 = 0x0004,

        /// <summary>
        /// 24 bit offset &lt;&lt; 2 &amp; sign ext.
        /// </summary>
        IMAGE_REL_M32R_PCREL24 = 0x0005,

        /// <summary>
        /// 16 bit offset &lt;&lt; 2 &amp; sign ext.
        /// </summary>
        IMAGE_REL_M32R_PCREL16 = 0x0006,

        /// <summary>
        /// 8 bit offset &lt;&lt; 2 &amp; sign ext.
        /// </summary>
        IMAGE_REL_M32R_PCREL8 = 0x0007,

        /// <summary>
        /// 16 MSBs
        /// </summary>
        IMAGE_REL_M32R_REFHALF = 0x0008,

        /// <summary>
        /// 16 MSBs; adj for LSB sign ext.
        /// </summary>
        IMAGE_REL_M32R_REFHI = 0x0009,

        /// <summary>
        /// 16 LSBs
        /// </summary>
        IMAGE_REL_M32R_REFLO = 0x000A,

        /// <summary>
        /// Link HI and LO
        /// </summary>
        IMAGE_REL_M32R_PAIR = 0x000B,

        /// <summary>
        /// Section table index
        /// </summary>
        IMAGE_REL_M32R_SECTION = 0x000C,

        /// <summary>
        /// 32 bit section relative reference
        /// </summary>
        IMAGE_REL_M32R_SECREL32 = 0x000D,

        /// <summary>
        /// clr token
        /// </summary>
        IMAGE_REL_M32R_TOKEN = 0x000E
    }
}
