using PESpy.View;

namespace PESpy.OMF
{
    //C5 is 32-bit
    //https://www.azillionmonkeys.com/qed/Omfg.pdf p66

    /// <summary>
    /// Symbol Line Numbers Record
    /// </summary>
    public readonly unsafe struct LINSYM : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => (OMFRecordType) (*value & ~1);

        public bool Is32Bit => (*value & 1) == 1;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content

        public byte Checksum => *(value + RecordLength + 2);

        public LINSYM(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LINSYM, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildren();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
