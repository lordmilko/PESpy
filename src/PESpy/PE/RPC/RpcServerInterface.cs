using System;
using System.Diagnostics;

namespace PESpy
{
    //RPC_SERVER_INTERFACE
    public readonly struct RpcServerInterface
    {
        private const int LengthOffset = 0;
        private const int InterfaceIdOffset = 4;
        internal const int TransferSyntaxOffset = 24;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int DispatchTableOffset => 44 + (chunk.Is32Bit ? 0 : 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int RpcProtseqEndpointCountOffset => 44 + (chunk.Is32Bit ? 4 : 8 + 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int RpcProtseqEndpointOffset => 48 + (chunk.Is32Bit ? 4 : 8 + 8);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int DefaultManagerEpvOffset => 48 + (chunk.Is32Bit ? (2 * 4) : (2 * 8) + 8);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int InterpreterInfoOffset => 48 + (chunk.Is32Bit ? (3 * 4) : (3 * 8) + 8);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int FlagsOffset => 48 + (chunk.Is32Bit ? (4 * 4) : (4 * 8) + 8);

        public int Length => chunk.PeekInt32(LengthOffset);

        public RpcSyntaxIdentifier InterfaceId => new RpcSyntaxIdentifier(chunk.Slice(InterfaceIdOffset));

        public RpcSyntaxIdentifier TransferSyntax => new RpcSyntaxIdentifier(chunk.Slice(TransferSyntaxOffset));

        public VA<RpcDispatchTable> DispatchTable => RpcInfo.ReadDispatchTable(chunk, DispatchTableOffset);

        public int RpcProtseqEndpointCount => chunk.PeekInt32(RpcProtseqEndpointCountOffset);

        public VA<RpcProtseqEndpoint> RpcProtseqEndpoint => RpcInfo.ReadRpcProtseqEndpoint(chunk, RpcProtseqEndpointOffset);

        public long DefaultManagerEpv => (long) chunk.PeekPointer(DefaultManagerEpvOffset);

        public VA<MidlServerInfo> InterpreterInfo
        {
            get
            {
                var va = (long) chunk.PeekPointer(InterpreterInfoOffset);

                if (va == 0)
                    return default;

                var peFile = chunk.PEFile();

                var rva = (int) (va - peFile.OptionalHeader.ImageBase);

                if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                {
                    var dispatchTable = DispatchTable;

                    var dispatchTableCount = dispatchTable.IsValid ? dispatchTable.Value.DispatchTableCount : 0;

                    return new VA<MidlServerInfo>(va, chunk.AbsoluteOffset, new MidlServerInfo(valueChunk, dispatchTableCount));
                }

                return new VA<MidlServerInfo>(va);
            }
        }

        public RPC_FLAGS Flags => (RPC_FLAGS) chunk.PeekInt32(FlagsOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal int StructSize(bool is32Bit) => throw new NotImplementedException();

        private readonly MemoryChunk chunk;

        internal RpcServerInterface(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return InterfaceId.ToString();
        }
    }
}
