#if PEFAST
namespace PESpy
{
    class LocalHeaderMemoryBlock : HeaderMemoryBlock
    {
        internal unsafe LocalHeaderMemoryBlock(byte* mmf, IMemoryBlockProvider provider) : base(provider)
        {
            //Some applications are only 1024, but some are 4096
            RemoteEndOffset = 0x1000;
            LocalPointer = mmf;
        }

        internal override void Resize(int newSize)
        {
            //As we are backed by a memory mapped file, we don't need to do anything
        }

        public override void Dispose(bool disposing)
        {
        }
    }
}
#endif
