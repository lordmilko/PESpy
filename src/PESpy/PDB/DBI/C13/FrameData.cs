using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct FrameData : IValue, IViewable
    {
        public int ulRvaStart => chunk.PeekInt32(0);
        public int cbBlock => chunk.PeekInt32(4);
        public int cbLocals => chunk.PeekInt32(8);
        public int cbParams => chunk.PeekInt32(12);
        public int cbStkMax => chunk.PeekInt32(16);
        public int frameFunc => chunk.PeekInt32(20);
        public short cbProlog => chunk.PeekInt16(24);
        public short cbSavedRegs => chunk.PeekInt16(26);

        public bool fHasSEH => (data & 1) != 0;

        public bool fHasEH => (data & (1 << 1)) != 0;

        public bool fIsFunctionStart => (data & (1 << 2)) != 0;

        public int reserved => data >> 3;

        private int data => chunk.PeekInt32(28);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //ulRvaStart
            sizeof(int) + //cbBlock
            sizeof(int) + //cbLocals
            sizeof(int) + //cbParams
            sizeof(int) + //cbStkMax
            sizeof(int) + //frameFunc
            sizeof(short) + //cbProlog
            sizeof(short) + //cbSavedRegs
            sizeof(int);    //data

        private readonly MemoryChunk chunk;

        internal FrameData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(FRAMEDATA), this, ViewKind.FrameData);

            s.WriteField(nameof(ulRvaStart), ulRvaStart);
            s.WriteField(nameof(cbBlock), cbBlock);
            s.WriteField(nameof(cbLocals), cbLocals);
            s.WriteField(nameof(cbParams), cbParams);
            s.WriteField(nameof(cbStkMax), cbStkMax);
            s.WriteField(nameof(frameFunc), frameFunc);
            s.WriteField(nameof(cbProlog), cbProlog);
            s.WriteField(nameof(cbSavedRegs), cbSavedRegs);
            
            using (var bitField = s.WriteBitFields<int>())
            {
                bitField.WriteField(nameof(fHasSEH), fHasSEH, 1);
                bitField.WriteField(nameof(fHasEH), fHasEH, 1);
                bitField.WriteField(nameof(fIsFunctionStart), fIsFunctionStart, 1);
                bitField.WriteField(nameof(reserved), reserved, 29);
            }
        }
    }
}
