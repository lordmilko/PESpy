namespace PESpy.PDB
{
    public enum CV_SIGNATURE
    {
        /// <summary>
        /// Actual signature is >64K
        /// </summary>
        C6 = 0,

        /// <summary>
        /// First explicit signature
        /// </summary>
        C7 = 1,

        /// <summary>
        /// C11 (vc5.x) 32-bit types
        /// </summary>
        C11 = 2,

        /// <summary>
        /// C13 (vc7.x) zero terminated names
        /// </summary>
        C13 = 4
    }
}
