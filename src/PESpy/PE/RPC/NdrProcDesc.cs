namespace PESpy
{
    //NDR_PROC_DESC
    public readonly struct NdrProcDesc
    {
        public short ClientBufferSize => chunk.PeekInt16(0);

        public short ServerBufferSize => chunk.PeekInt16(2);

        public INTERPRETER_OPT_FLAGS Oi2Flags => (INTERPRETER_OPT_FLAGS) chunk.PeekByte(4);

        public byte NumberParams => chunk.PeekByte(5);

        public NdrProcHeaderExts NdrExts => new NdrProcHeaderExts(chunk.Slice(6));

        private readonly MemoryChunk chunk;

        internal NdrProcDesc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
