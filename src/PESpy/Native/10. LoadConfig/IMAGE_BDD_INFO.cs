namespace PESpy.Native
{
    //ImageBDDInfo
    internal struct IMAGE_BDD_INFO
    {
        public int Version;      // decides the semantics of serialized BDD
        public int BDDSize;
        // IMAGE_BDD_DYNAMIC_RELOCATION BDDNodes[ANYSIZE_ARRAY]; // BDDSize size in bytes.
    }
}