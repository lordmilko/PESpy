namespace PESpy
{
    //NDR_PROC_HEADER_EXTS
    public readonly struct NdrProcHeaderExts
    {
        public byte Size => chunk.PeekByte(0);

        public INTERPRETER_OPT_FLAGS2 Flags2 => (INTERPRETER_OPT_FLAGS2) chunk.PeekByte(1);

        public short ClientCorrHint => chunk.PeekInt16(2);

        public short ServerCorrHint => chunk.PeekInt16(4);

        public short NotifyIndex => chunk.PeekInt16(6);

        public short FloatArgMask => chunk.PeekInt16(8);

        private readonly MemoryChunk chunk;

        internal NdrProcHeaderExts(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
