namespace PESpy.NE
{
    //new_rsrc
    public class NewRsrc //May not be present
    {
        public ushort rs_align => chunk.PeekUInt16(0);

        public RsrcTypeInfo rs_typeinfo => new RsrcTypeInfo(chunk.Slice(2));

        private readonly MemoryChunk chunk;

        internal NewRsrc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
