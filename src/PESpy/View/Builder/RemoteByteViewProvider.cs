using System;

namespace PESpy.View.Builder
{
    //Needs to move between sections, potentially downloading data from the remote process as we go
    internal class RemoteByteViewProvider : ByteViewProvider
    {
        private PEFile peFile;

        //This property should not be accessed when we're in virtual mode
        public override int FileOrSectionLength => throw new NotSupportedException();

        public RemoteByteViewProvider(PEFile peFile, FileAccessor fileAccessor) : base(fileAccessor, isLibFile: false)
        {
            this.peFile = peFile;
        }

        protected override unsafe (IntPtr pBytes, int memoryLength, int relativeOffset) AcquireMemory(int targetAddress)
        {
            //Lookup the section associated with this RVA

            if (!peFile.TryGetValueChunkFromSectionOrHeader(targetAddress, out var chunk))
                throw new NotImplementedException();

            return ((IntPtr) chunk.block.LocalPointer, chunk.block.Length, chunk.RelativeOffset);
        }
    }
}
