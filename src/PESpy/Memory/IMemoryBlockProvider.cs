namespace PESpy
{
    interface IMemoryBlockProvider
    {
        MemoryBlock CreateBlock(int offsetOrRVA, int size);
    }

    interface IFileMemoryBlockProvider : IMemoryBlockProvider
    {
        IFile File { get; }
    }
}
