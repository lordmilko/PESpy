using System.Diagnostics;

namespace PESpy
{
    //MIDL_STUBLESS_PROXY_INFO
    public readonly struct MidlStublessProxyInfo
    {
        private const int pStubDescOffset = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ProcFormatStringOffset => chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int FormatStringOffsetOffset => 2 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int pTransferSyntaxOffset => 3 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int nCountOffset => 4 * chunk.PointerSize;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int pSyntaxInfoOffset => 5 * chunk.PointerSize;

        public long pStubDesc => (long) chunk.PeekPointer(pStubDescOffset);

        public RpcFormatString ProcFormatString => RpcFormatString.New(chunk, ProcFormatStringOffset, FormatStringOffset);

        public VA<NativeSpan<ushort>> FormatStringOffset => RpcInfo.ReadFormatStringOffsets(chunk, FormatStringOffsetOffset, ProcFormatStringOffset);

        public VA<RpcSyntaxIdentifier> pTransferSyntax => RpcInfo.ReadTransferSyntax(chunk, pTransferSyntaxOffset);

        public long nCount => (long) chunk.PeekPointer(nCountOffset);

        public VA<MidlSyntaxInfo[]> pSyntaxInfo => RpcInfo.ReadSyntaxInfo(chunk, pSyntaxInfoOffset, nCount);

        private readonly MemoryChunk chunk;

        internal MidlStublessProxyInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
