namespace PESpy.OMF
{
    public readonly unsafe struct THEADR
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content
        public FixedAnsiString Name
        {
            get
            {
                var strLen = *(value + 3);

                return new FixedAnsiString(value + 4, strLen);
            }
        }

        public byte Checksum => *(value + RecordLength + 2);

        public THEADR(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
