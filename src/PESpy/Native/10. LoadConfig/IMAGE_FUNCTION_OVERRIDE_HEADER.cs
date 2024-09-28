namespace PESpy.Native
{
    //ImageFunctionOverrideHeader
    internal struct IMAGE_FUNCTION_OVERRIDE_HEADER
    {
        public int FuncOverrideSize;

        //IMAGE_FUNCTION_OVERRIDE_DYNAMIC_RELOCATION  FuncOverrideInfo[ANYSIZE_ARRAY]; // FuncOverrideSize bytes in size
        //IMAGE_BDD_INFO BDDInfo; // BDD region, size in bytes: DVRTEntrySize - sizeof(IMAGE_FUNCTION_OVERRIDE_HEADER) - FuncOverrideSize
    }
}