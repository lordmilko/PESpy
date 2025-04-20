namespace PESpy
{
    interface IMemoryBlockProvider
    {
        MemoryBlock CreateBlock(int offsetOrRVA, int size);
    }

    interface IFileMemoryBlockProvider<TFile> : IMemoryBlockProvider
    {
        TFile File { get; }
    }
}
