namespace PESpy
{
    //CORCOMPILE_CODE_MANAGER_ENTRY
    [Source(SourceKind.corcompile_h)]
    public class CorCompileCodeManagerEntry : IValue //May not be present
    {
        private const int HotCodeOffset = 0;
        private const int CodeOffset = 8;
        private const int ColdCodeOffset = 16;
        private const int RODataOffset = 24;
        private const int HotIBCMethodOffsetOffset = 32;
        private const int HotGenericsMethodOffsetOffset = 36;
        private const int ColdUntrainedMethodOffsetOffset = 40;

        public ImageDataDirectory HotCode => new ImageDataDirectory(chunk);

        //nidump.cpp says that this code is "unprofiled" code
        public ImageDataDirectory Code => new ImageDataDirectory(chunk.Slice(CodeOffset));

        public ImageDataDirectory ColdCode => new ImageDataDirectory(chunk.Slice(ColdCodeOffset));

        public ImageDataDirectory ROData => new ImageDataDirectory(chunk.Slice(RODataOffset));

        //Layout is
        //HOT COMMON
        //HOT IBC
        //HOT GENERICS
        //Hot due to procedure splitting

        public int HotIBCMethodOffset => chunk.PeekInt32(HotIBCMethodOffsetOffset);

        public int HotGenericsMethodOffset => chunk.PeekInt32(HotGenericsMethodOffsetOffset);

        //COLD IBC
        //Cold due to procedure splitting.

        public int ColdUntrainedMethodOffset => chunk.PeekInt32(ColdUntrainedMethodOffsetOffset);

        public long Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal CorCompileCodeManagerEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
