namespace PESpy
{
    //https://www.pcjs.org/documents/books/mspl13/c/ctoolkit/ section 1.4

    //Name is made up
    public readonly unsafe struct OldTypType
    {
        private readonly byte* value;

        public bool linkage => *value != 0;

        public ushort len => *(ushort*) (value + 1);

        //What follows is described as a "type string", which is a series of consecutive leaves

        //Strictly speaking this is just the type of the first leaf, but that's actually the same way that TYPTYPE
        //works; it's not that there is a string of "consecutive" leaves, but rather certain leaf types contain
        //other leaf types within them
        public OLF leaf => *(OLF*) (value + 3);

        //All the data that follows the leaf kind
        public NativeSpan<byte> Data => new NativeSpan<byte>(value + 4, len - 1);

        internal OldTypType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return leaf.ToString();
        }
    }
}
