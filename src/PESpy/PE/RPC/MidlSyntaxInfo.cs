using System.Diagnostics;

namespace PESpy
{
    //MIDL_SYNTAX_INFO
    public readonly struct MidlSyntaxInfo
    {
        private const int TransferSyntaxOffset = 0;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int DispatchTableOffset => 20 + (chunk.Is32Bit ? 0 : 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int ProcStringOffset => 20 + (chunk.Is32Bit ? 4 : 8 + 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int FmtStringOffsetOffset => 20 + (chunk.Is32Bit ? 2 * 4 : 2 * 8 + 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int TypeStringOffset => 20 + (chunk.Is32Bit ? 3 * 4 : 3 * 8 + 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int aUserMarshalQuadrupleOffset => 20 + (chunk.Is32Bit ? 4 * 4 : 4 * 8 + 4);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int pMethodPropertiesOffset => 20 + (chunk.Is32Bit ? 5 * 4 : 5 * 8 + 4);
        private int pReserved2Offset => 20 + (chunk.Is32Bit ? 6 * 4 : 6 * 8 + 4);

        public RpcSyntaxIdentifier TransferSyntax => new RpcSyntaxIdentifier(chunk);

        public VA<RpcDispatchTable> DispatchTable => RpcInfo.ReadDispatchTable(chunk, DispatchTableOffset);

        //This field is not used in NDR64; see FmtStringOffset64 instead
        public RpcFormatString ProcString
        {
            get
            {
                if (TransferSyntax.SyntaxGUID == RpcInfo.NDR64TransferSyntax)
                    return null;

                return RpcFormatString.New(chunk, ProcStringOffset, FmtStringOffset);
            }
        }

        //IDA Pro indicates, and NdrpGetProcString confirms that in NDR64 the MIDL_SYNTAX_INFO.FmtStringOffset is in fact a series of VAs
        //to PFORMAT_STRING entities. As such, this field is not used in NDR64
        public VA<NativeSpan<ushort>> FmtStringOffset
        {
            get
            {
                if (TransferSyntax.SyntaxGUID == RpcInfo.NDR64TransferSyntax)
                    return default;

                return RpcInfo.ReadFormatStringOffsets(chunk, FmtStringOffsetOffset, ProcStringOffset);
            }
        }

        //The field is FmtStringOffset, but the content is radically different when we're NDR64
        public VA<VA<Ndr64ProcFormat>[]> FmtStringOffset64
        {
            get
            {
                if (TransferSyntax.SyntaxGUID != RpcInfo.NDR64TransferSyntax)
                    return default;

                return RpcInfo.ReadFormatStringOffsets64(chunk, FmtStringOffsetOffset);
            }
        }

        public long TypeString => (long) chunk.PeekPointer(TypeStringOffset);

        public long aUserMarshalQuadruple => (long) chunk.PeekPointer(aUserMarshalQuadrupleOffset);

        public long pMethodProperties => (long) chunk.PeekPointer(pMethodPropertiesOffset);

        public long pReserved2 => (long) chunk.PeekPointer(pReserved2Offset);

        public static int StructSize(bool is32Bit) =>
            RpcSyntaxIdentifier.StructSize +
            (is32Bit ? 7 * 4 : 7 * 8 + 4);

        private readonly MemoryChunk chunk;

        internal MidlSyntaxInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
