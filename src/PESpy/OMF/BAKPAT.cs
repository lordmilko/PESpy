using PESpy.View;

namespace PESpy.OMF
{
    //B3 is 32-bit
    //https://www.azillionmonkeys.com/qed/Omfg.pdf p57

    /// <summary>
    /// Backpatch Record
    /// </summary>
    public readonly unsafe struct BAKPAT : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => (OMFRecordType) (*value & ~1);

        public bool Is32Bit => (*value & 1) == 1;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content

        public byte Checksum => *(value + RecordLength + 2);

        public BAKPAT(byte* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return RecordType.ToString();
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.BAKPAT, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildren();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);
    }
}
