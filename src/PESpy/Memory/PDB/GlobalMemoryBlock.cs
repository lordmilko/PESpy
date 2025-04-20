namespace PESpy
{
    internal class GlobalMemoryBlock : MemoryBlock
    {
        public override int Length { get; }

        public unsafe GlobalMemoryBlock(byte* mmf, int length) : base(null)
        {
            LocalPointer = mmf;
            Length = length;
        }

        public override void Dispose(bool disposing)
        {
            //Nothing we do; we don't own the memory
        }
    }
}
