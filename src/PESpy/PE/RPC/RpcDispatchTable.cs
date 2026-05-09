using System.Diagnostics;

namespace PESpy
{
    //RPC_DISPATCH_TABLE
    public readonly struct RpcDispatchTable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private const int DispatchTableCountOffset = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int DispatchTableOffset => 4 + (chunk.Is32Bit ? 0 : 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ReservedOffset => 4 + (chunk.Is32Bit ? 4 : 8 + 4);

        public int DispatchTableCount => chunk.PeekInt32(DispatchTableCountOffset);

        //This is a bunch of NdrServerCall2 function calls. The interesting stuff is in the MidlServerInfo
        public VA<long[]> DispatchTable => RpcInfo.ReadPointers(chunk, DispatchTableOffset, DispatchTableCount);

        public long Reserved => (long) chunk.PeekPointer(ReservedOffset);

        private readonly MemoryChunk chunk;

        internal RpcDispatchTable(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
