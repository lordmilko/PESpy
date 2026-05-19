using PESpy.View;

namespace PESpy.OMF
{
    /// <summary>
    /// Comment Record (Including all comment class extensions)
    /// </summary>
    public readonly unsafe struct COMENT : IViewable
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.COMENT, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildren();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return ByteString.ToString();
        }
    }
}
