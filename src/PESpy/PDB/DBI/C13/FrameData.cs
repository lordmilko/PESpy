using System;
using PESpy.View;

namespace PESpy.PDB
{
    public readonly struct FrameData : IValue, IViewable
    {
        private const int ulRvaStartOffset = 0;
        private const int cbBlockOffset = 4;
        private const int cbLocalsOffset = 8;
        private const int cbParamsOffset = 12;
        private const int cbStkMaxOffset = 16;
        private const int frameFuncOffset = 20;
        private const int cbPrologOffset = 24;
        private const int cbSavedRegsOffset = 26;
        private const int dataOffset = 28;
        public int ulRvaStart => chunk.PeekInt32(ulRvaStartOffset);
        public int cbBlock => chunk.PeekInt32(cbBlockOffset);
        public int cbLocals => chunk.PeekInt32(cbLocalsOffset);
        public int cbParams => chunk.PeekInt32(cbParamsOffset);
        public int cbStkMax => chunk.PeekInt32(cbStkMaxOffset);
        public int frameFunc => chunk.PeekInt32(frameFuncOffset);
        public short cbProlog => chunk.PeekInt16(cbPrologOffset);
        public short cbSavedRegs => chunk.PeekInt16(cbSavedRegsOffset);

        public bool fHasSEH => (data & 1) != 0;

        public bool fHasEH => (data & (1 << 1)) != 0;

        public bool fIsFunctionStart => (data & (1 << 2)) != 0;

        public int reserved => data >> 3;

        private int data => chunk.PeekInt32(dataOffset);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FRAMEDATA, this, ViewKind.FrameData, StructSize);

        int IViewable.NumChildren => 12;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(ulRvaStart), ulRvaStartOffset, ulRvaStart);
                    break;

                case 1:
                    structWriter.WriteField(nameof(cbBlock), cbBlockOffset, cbBlock);
                    break;

                case 2:
                    structWriter.WriteField(nameof(cbLocals), cbLocalsOffset, cbLocals);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbParams), cbParamsOffset, cbParams);
                    break;

                case 4:
                    structWriter.WriteField(nameof(cbStkMax), cbStkMaxOffset, cbStkMax);
                    break;

                case 5:
                    structWriter.WriteField(nameof(frameFunc), frameFuncOffset, frameFunc);
                    break;

                case 6:
                    structWriter.WriteField(nameof(cbProlog), cbPrologOffset, cbProlog);
                    break;

                case 7:
                    structWriter.WriteField(nameof(cbSavedRegs), cbSavedRegsOffset, cbSavedRegs);
                    break;

                #region BitField

                case 8:
                    structWriter.WriteBitField(nameof(fHasSEH), dataOffset, fHasSEH, sizeof(int), 1);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(fHasEH), dataOffset, fHasEH, sizeof(int), 1);
                    break;

                case 10:
                    structWriter.WriteBitField(nameof(fIsFunctionStart), dataOffset, fIsFunctionStart, sizeof(int), 1);
                    break;

                case 11:
                    structWriter.WriteBitField(nameof(reserved), dataOffset, reserved, sizeof(int), 29);
                    break;

                #endregion

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
