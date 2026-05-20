using PESpy.View;

namespace PESpy.OMF
{
    /// <summary>
    /// List of Names Record
    /// </summary>
    public readonly unsafe struct LNAMES : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content
        public FixedAnsiString[] Names
        {
            get
            {
                var length = RecordLength;
                var end = value + length - 1;

                var ptr = value + 3;

                using var results = new ValueList<FixedAnsiString>();

                while (ptr < end)
                {
                    var strLen = *ptr;
                    var str = new FixedAnsiString(ptr + 1, strLen);

                    results.Add(str);

                    ptr += strLen + 1;
                }

                return results.ToArray();
            }
        }

        public byte Checksum => *(value + RecordLength + 2);

        public LNAMES(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LNAMES, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildren();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
