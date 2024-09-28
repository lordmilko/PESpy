namespace PESpy.Native
{
    //ImageFunctionOverrideDynamicRelocation
    internal struct IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION
    {
        public int OriginalRva;          // RVA of original function
        public int BDDOffset;            // Offset into the BDD region
        public int RvaSize;              // Size in bytes taken by RVAs. Must be multiple of sizeof(int).
        public int BaseRelocSize;        // Size in bytes taken by BaseRelocs

        // int RVAs[RvaSize / sizeof(int)];     // Array containing overriding func RVAs. 

        // IMAGE_BASE_RELOCATION  BaseRelocs[ANYSIZE_ARRAY]; // Base relocations (RVA + Size + TO)
        //  Padded with extra TOs for 4B alignment
        // BaseRelocSize size in bytes
    }
}