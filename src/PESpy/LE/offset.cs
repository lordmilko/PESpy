namespace PESpy.LE
{
    public readonly struct offset
    {
        //It's a union
        public short offset16 { get; }

        public int offset32 { get; }

        internal offset(int offset32)
        {
            this.offset32 = offset32;
        }

        internal offset(short offset16)
        {
            this.offset16 = offset16;
        }
    }
}
