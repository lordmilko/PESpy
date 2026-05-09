using System;

namespace PESpy
{
    //RPC_SYNTAX_IDENTIFIER
    public readonly struct RpcSyntaxIdentifier
    {
        public Guid SyntaxGUID => chunk.PeekGuid(0);

        public RpcVersion SyntaxVersion => new RpcVersion(chunk.Slice(16));

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            16 + //SyntaxGUID
            RpcVersion.StructSize;

        private readonly MemoryChunk chunk;

        internal RpcSyntaxIdentifier(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return $"{SyntaxGUID}:{SyntaxVersion}";
        }
    }
}
