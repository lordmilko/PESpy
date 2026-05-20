using PESpy.View;

namespace PESpy.OMF
{
    //A1 is 32-bit
    //https://www.azillionmonkeys.com/qed/Omfg.pdf p49

    /// <summary>
    /// Logical Enumerated Data Record
    /// </summary>
    public readonly unsafe struct LEDATA : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => (OMFRecordType) (*value & ~1);

        public bool Is32Bit => (*value & 1) == 1;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content
        public ushort SegmentIndex => OMFRecord.GetIndex(value + 3, out _);

        public int EnumeratedDataOffset
        {
            get
            {
                OMFRecord.GetIndex(value + 3, out var indexSize);

                var off = value + 3 + indexSize;

                if (Is32Bit)
                    return *(int*) off;

                return *(ushort*) off;
            }
        }

        public NativeSpan<byte> DataBytes
        {
            get
            {
                OMFRecord.GetIndex(value + 3, out var indexSize);

                var off = indexSize + (Is32Bit ? 4 : 2);

                return new NativeSpan<byte>(value + 3 + off, RecordLength - off - 1);
            }
        }

        public byte Checksum => *(value + RecordLength + 2);

        public LEDATA(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LEDATA, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildren();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
