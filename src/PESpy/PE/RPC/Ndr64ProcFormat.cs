namespace PESpy
{
    //NDR64_PROC_FORMAT
    public readonly struct Ndr64ProcFormat
    {
        public NDR64_PROC_FLAGS Flags => chunk.PeekUInt32(0);

        public int StackSize => chunk.PeekInt32(4);

        public int ConstantClientBufferSize => chunk.PeekInt32(8);

        public int ConstantServerBufferSize => chunk.PeekInt32(12);

        public short RpcFlags => chunk.PeekInt16(16);

        public short FloatDoubleMask => chunk.PeekInt16(18);

        public short NumberOfParams => chunk.PeekInt16(20);

        public short ExtensionSize => chunk.PeekInt16(22);

        public NativeSpan<byte> ExtensionData => chunk.PeekNativeSpan<byte>(StructSize, ExtensionSize);

        public Ndr64ParamFormat[] Parameters
        {
            get
            {
                var read = StructSize + ExtensionSize;

                var parameters = new Ndr64ParamFormat[NumberOfParams];

                var paramSize = Ndr64ParamFormat.StructSize(chunk.Is32Bit);

                for (var i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = new Ndr64ParamFormat(chunk.Slice(read));
                    read += paramSize;
                }

                return parameters;
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //Flags
            sizeof(int) + //StackSize
            sizeof(int) + //ConstantClientBufferSize
            sizeof(int) + //ConstantServerBufferSize
            sizeof(short) + //RpcFlags
            sizeof(short) + //FloatDoubleMask
            sizeof(short) + //NumberOfParams
            sizeof(short); //ExtensionSize

        private readonly MemoryChunk chunk;

        internal Ndr64ProcFormat(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
