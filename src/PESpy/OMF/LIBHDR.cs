using PESpy.View;

namespace PESpy.OMF
{
    public readonly unsafe struct LIBHDR : IViewable
    {
        private readonly byte* value;

        public OMFRecordType RecordType => *(OMFRecordType*) value;

        public ushort RecordLength => *(ushort*) (value + 1);

        public int DictionaryOffset => *(int*) (value + 3);

        public ushort DictionaryBlockCount => *(ushort*) (value + 7);

        public byte Flags => *(value + 9);

        public int PageSize => RecordLength + 3;

        public NativeSpan<byte> Padding => new NativeSpan<byte>(value + 10, PageSize - 10);

        public LIBHDR(byte* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LIBHDR, OMFRecord.GetStructSize(value));

        int IViewable.NumChildren() => OMFRecord.GetDefaultNumChildrenNoChecksum();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) =>
            OMFRecord.WriteDefaultChild(new OMFRecord(value), index, ref structWriter);

        public override string ToString()
        {
            return RecordType.ToString();
        }
    }
}
