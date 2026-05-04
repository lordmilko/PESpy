namespace PESpy.NE
{
    //new_rsrc
    public class new_rsrc //May not be present
    {
        public ushort rs_align => chunk.PeekUInt16(0);

        public rsrc_typeinfo rs_typeinfo => new rsrc_typeinfo(chunk.Slice(2));

        private readonly MemoryChunk chunk;

        internal new_rsrc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
