namespace PESpy
{
    interface IMemoryBlockProvider
    {
        int StartOffset { get; }

        MemoryBlock CreateBlock(int offsetOrRVA, int size);
    }

    interface IFileMemoryBlockProvider : IMemoryBlockProvider
    {
        IFile File { get; }
    }
}
