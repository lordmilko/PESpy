namespace PESpy.OMF
{
    //Demarcates the end of the main library content, and contains padding up to the start of the library dictionary
    public readonly unsafe struct DICHDR
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        public NativeSpan<byte> Padding => new NativeSpan<byte>(value + 3, RecordLength);

        public DICHDR(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
