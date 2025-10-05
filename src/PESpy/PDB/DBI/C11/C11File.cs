namespace PESpy.PDB
{
    //Type is made up
    public readonly struct C11File
    {
        public SPB SPB => new SPB(chunk);

        public SE StartEnd => chunk.PeekUnmanaged<SE>(SPB.StructSize);

        public unsafe FixedAnsiString FileName
        {
            get
            {
                var offset = SPB.StructSize + sizeof(SE);

                var length = chunk.PeekByte(offset);
                return chunk.PeekAnsiFixedLength(offset + 1, length);
            }
        }

        //FileName needs to be 32-bit aligned

        public unsafe SPO SPO
        {
            get
            {
                var offset = SPB.StructSize + sizeof(SE);

                var length = chunk.PeekByte(offset);

                offset += length + 1;

                return new SPO(chunk.Slice(offset));
            }
        }

        //If cPair is odd, align another short

        private readonly MemoryChunk chunk;

        internal C11File(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return FileName.ToString();
        }
    }
}
