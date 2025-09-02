namespace PESpy
{
    //Name is made up
    public readonly unsafe struct OldSymType
    {
        private const byte MaskIs32Bit = 0x80;

        private readonly byte* value;

        public byte reclen => *value;

        public OLDSYM rectyp => (OLDSYM) (*(value + 1) & ~MaskIs32Bit);

        public bool Is32Bit => (*(value + 1) & MaskIs32Bit) != 0;

        internal OldSymType(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return rectyp.ToString();
        }
    }
}
