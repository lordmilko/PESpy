namespace PESpy.View.Builder
{
    //Needs to move between sections, potentially downloading data from the remote process as we go
    internal class RemoteByteViewProvider : ByteViewProvider
    {
        private PEFile peFile;
        private ImageSectionHeader[] sectionHeaders;
        private int currentIndex = 1;

        public RemoteByteViewProvider(PEFile peFile, ImageSectionHeader[] sectionHeaders, IViewDisassembler? viewDisassembler) : base(viewDisassembler)
        {
            this.peFile = peFile;
            this.sectionHeaders = sectionHeaders;
        }
    }
}
