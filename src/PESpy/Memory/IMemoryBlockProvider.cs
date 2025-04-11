namespace PESpy
{
    interface IMemoryBlockProvider
    {
        PEFile PEFile { get; }

        MemoryBlock CreateBlock(int offsetOrRVA, int size);
    }
}
