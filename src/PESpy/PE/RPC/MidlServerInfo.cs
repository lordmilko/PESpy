using System.Diagnostics;

namespace PESpy
{
    //MIDL_SERVER_INFO
    public readonly struct MidlServerInfo
    {
        private const int pStubDescOffset = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int DispatchTableOffset => chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ProcStringOffset => 2 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int FmtStringOffsetOffset => 3 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ThunkTableOffset => 4 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int pTransferSyntaxOffset => 5 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int nCountOffset => 6 * chunk.PointerSize;
        private int pSyntaxInfoOffset => 7 * chunk.PointerSize;

        public long pStubDesc => (long) chunk.PeekPointer(pStubDescOffset); //PMIDL_STUB_DESC

        //Unlike the dispatch table on the server interface, this is merely a list of pointers
        public VA<long[]> DispatchTable => RpcInfo.ReadPointers(chunk, DispatchTableOffset, dispatchTableCount);

        public RpcFormatString ProcString => RpcFormatString.New(chunk, ProcStringOffset, FmtStringOffset);

        public VA<NativeSpan<ushort>> FmtStringOffset => RpcInfo.ReadFormatStringOffsets(chunk, FmtStringOffsetOffset, ProcStringOffset);

        public long ThunkTable => (long) chunk.PeekPointer(ThunkTableOffset);

        public VA<RpcSyntaxIdentifier> pTransferSyntax => RpcInfo.ReadTransferSyntax(chunk, pTransferSyntaxOffset);

        public long nCount => (long) chunk.PeekPointer(nCountOffset);

        public VA<MidlSyntaxInfo[]> pSyntaxInfo => RpcInfo.ReadSyntaxInfo(chunk, pSyntaxInfoOffset, nCount);

        private readonly MemoryChunk chunk;
        private readonly int dispatchTableCount;

        internal MidlServerInfo(in MemoryChunk chunk, int dispatchTableCount)
        {
            this.chunk = chunk;
            this.dispatchTableCount = dispatchTableCount;
        }
    }
}
