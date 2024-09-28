namespace PESpy.Native
{
    //ImageBaseRelocation
    internal struct IMAGE_BASE_RELOCATION
    {
        public int VirtualAddress;
        public int SizeOfBlock; //Describes the entire size of this structure, including the VirtualAddress, SizeOfBlock and all of the TypeOffset entries

        //https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#base-relocation-block
        //  WORD    TypeOffset[1];
    }
}
