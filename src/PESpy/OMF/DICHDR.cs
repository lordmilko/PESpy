using PESpy.View;

namespace PESpy.OMF
{
    //Demarcates the end of the main library content, and contains padding up to the start of the library dictionary
    public readonly unsafe struct DICHDR : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        public NativeSpan<byte> Padding => new NativeSpan<byte>(value + 3, RecordLength);

        public DICHDR(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.DICHDR, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildrenNoChecksum();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) =>
            OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
