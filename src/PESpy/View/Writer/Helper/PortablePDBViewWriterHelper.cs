using PESpy.Ecma335;

namespace PESpy.View
{
    class PortablePDBViewWriterHelper : SimpleViewWriterHelper, IMetadataViewWriterHelper
    {
        private MetadataSizes metadataSizes;
        private bool hasMetadataSizes;

        public MetadataSizes MetadataReader
        {
            get
            {
                if (!hasMetadataSizes)
                {
                    metadataSizes = ((PortablePDBFile) file).EcmaMetadata.ModelHeap!.Sizes;
                    hasMetadataSizes = true;
                }

                return metadataSizes;
            }
        }

        internal PortablePDBViewWriterHelper(PortablePDBFile portablePDBFile) : base(portablePDBFile)
        {
        }
    }
}
