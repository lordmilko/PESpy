using PESpy.View;

namespace PESpy.PDB
{
    //Typically OMF* structs are only used for older CodeView formats, however OMFSegMap is definitely what is used
    //by DBI. The data is deserialized from the PDB into bufSecMap which is regularly casted to OMFSegMap*
    public readonly struct OMFSegMapDesc : IValue, IViewable
    {
        /// <summary>
        /// descriptor flags bit field.
        /// </summary>
        public OMFSegMapFlags flags => chunk.PeekUInt16(0);

        /// <summary>
        /// the logical overlay number
        /// </summary>
        public short ovl => chunk.PeekInt16(2);

        /// <summary>
        /// group index into the descriptor array
        /// </summary>
        public short group => chunk.PeekInt16(4);

        /// <summary>
        /// logical segment index - interpreted via flags
        /// </summary>
        public short frame => chunk.PeekInt16(6);

        /// <summary>
        /// segment or group name - index into sstSegName
        /// </summary>
        public short iSegName => chunk.PeekInt16(8);

        /// <summary>
        /// class name - index into sstSegName
        /// </summary>
        public short iClassName => chunk.PeekInt16(10);

        /// <summary>
        /// byte offset of the logical within the physical segment
        /// </summary>
        public int offset => chunk.PeekInt16(12);

        /// <summary>
        /// byte count of the logical segment or group
        /// </summary>
        public int cbSeg => chunk.PeekInt16(16);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //flags
            sizeof(short) + //ovl
            sizeof(short) + //group
            sizeof(short) + //frame
            sizeof(short) + //iSegName
            sizeof(short) + //iClassName
            sizeof(int) + //offset
            sizeof(int);

        private readonly MemoryChunk chunk;

        internal OMFSegMapDesc(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(OMFSegMapDesc), this, ViewKind.OMFSegMapDesc);

            using (var bitField = s.WriteBitFields<ushort>())
            {
                bitField.WriteField("fRead", flags.fRead, 1);
                bitField.WriteField("fWrite", flags.fWrite, 1);
                bitField.WriteField("fExecute", flags.fExecute, 1);
                bitField.WriteField("f32Bit", flags.f32Bit, 1);
                bitField.WriteField("res1", flags.res1, 4);
                bitField.WriteField("fSel", flags.fSel, 1);
                bitField.WriteField("fAbs", flags.fAbs, 1);
                bitField.WriteField("res2", flags.res2, 2);
                bitField.WriteField("fGroup", flags.fRead, 1);
                bitField.WriteField("res3", flags.res3, 3);
            }

            s.WriteField(nameof(ovl), ovl);
            s.WriteField(nameof(group), group);
            s.WriteField(nameof(frame), frame);
            s.WriteField(nameof(iSegName), iSegName);
            s.WriteField(nameof(iClassName), iClassName);
            s.WriteField(nameof(offset), offset);
            s.WriteField(nameof(cbSeg), cbSeg);
        }
    }
}
