namespace PESpy.OMF
{
    //95 is 32-bit
    //https://www.azillionmonkeys.com/qed/Omfg.pdf p34

    public readonly unsafe struct LINNUM
    {
        private readonly byte* value;

        public OMFRecordType RecordType => (OMFRecordType) (*value & ~1);

        public bool Is32Bit => (*value & 1) == 1;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content

        public byte Checksum => *(value + RecordLength + 2);

        public LINNUM(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
