using PESpy.View;

namespace PESpy.OMF
{
    /// <summary>
    /// Translator Header Record
    /// </summary>
    public readonly unsafe struct THEADR : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        //Content
        public SymString Name => new SymString(value + 4, true);

        public byte Checksum => *(value + RecordLength + 2);

        public THEADR(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.THEADR, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildren();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
