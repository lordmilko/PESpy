namespace PESpy.OMF
{
    public readonly unsafe struct COMENT
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content
        public byte CommentType => *(value + 3); //Top bit: NP (no purge bit). Next bit: NL (no list bit)

        public OMFCommentClass CommentClass => *(OMFCommentClass*) (value + 4);

        public FixedAnsiString ByteString => new FixedAnsiString(value + 5, RecordLength - 3);

        public byte Checksum => *(value + RecordLength + 2);

        public COMENT(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return ByteString.ToString();
        }
    }
}
