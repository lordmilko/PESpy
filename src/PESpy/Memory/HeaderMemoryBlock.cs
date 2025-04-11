namespace PESpy
{
    abstract class HeaderMemoryBlock : MemoryBlock
    {
        protected HeaderMemoryBlock(IMemoryBlockProvider provider) : base(provider)
        {
        }

        internal abstract void Resize(int newSize);
    }
}
