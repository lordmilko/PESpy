namespace PESpy.OMF
{
    public readonly unsafe struct CEXTDEF
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content

        public byte Checksum => *(value + RecordLength + 2);

        public CEXTDEF(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}