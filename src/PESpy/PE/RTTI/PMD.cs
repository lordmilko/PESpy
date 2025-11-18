namespace PESpy
{
    /// <summary>
    /// PMD - Pointer to Member Data: generalized pointer-to-member descriptor
    /// </summary>
    public struct PMD
    {
        /// <summary>
        /// Offset of intended data within base
        /// </summary>
        public int mdisp;

        /// <summary>
        /// Displacement to virtual base pointer
        /// </summary>
        public int pdisp;

        /// <summary>
        /// Index within vbTable to offset of base
        /// </summary>
        public int vdisp;

        internal const int StructSize =
            sizeof(int) + //mdisp
            sizeof(int) + //pdisp
            sizeof(int); //vdisp
    }
}
