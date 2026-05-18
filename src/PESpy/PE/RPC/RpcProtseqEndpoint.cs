namespace PESpy
{
    //RPC_PROTSEQ_ENDPOINT
    public readonly struct RpcProtseqEndpoint
    {
        public VA<AnsiString> RpcProtocolSequence
        {
            get
            {
                var va = (long) chunk.PeekPointer(0);

                var peFile = chunk.PEFile();

                if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                {
                    var str = valueChunk.PeekAnsiNullTerminatedString(0);
                    return new VA<AnsiString>(va, valueChunk.AbsoluteOffset, str);
                }

                return default;
            }
        }

        public VA<AnsiString> Endpoint
        {
            get
            {
                var va = (long) chunk.PeekPointer(4);

                var peFile = chunk.PEFile();

                if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                {
                    var str = valueChunk.PeekAnsiNullTerminatedString(0);
                    return new VA<AnsiString>(va, valueChunk.AbsoluteOffset, str);
                }

                return default;
            }
        }

        private readonly MemoryChunk chunk;

        internal RpcProtseqEndpoint(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
