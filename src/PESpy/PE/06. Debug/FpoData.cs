using System;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="FPO_DATA"/> structure that describes stack frame layout for a function on an x86 computer
    /// when frame pointer omission (FPO) optimization is used. The structure is used to locate the base of the call frame.
    /// </summary>
    public readonly struct FpoData : IValue, IViewable
    {
        private const int OffStartOffset = 0;
        private const int ProcSizeOffset = 4;
        private const int LocalsOffset = 8;
        private const int ParamsOffset = 12;
        private const int flagsOffset = 14;

        /// <summary>
        /// The offset of the first byte of the function code.
        /// </summary>
        public int OffStart => chunk.PeekInt32(OffStartOffset);

        /// <summary>
        /// The number of bytes in the function.
        /// </summary>
        public int ProcSize => chunk.PeekInt32(ProcSizeOffset);

        /// <summary>
        /// The number of local variables.
        /// </summary>
        public int Locals => chunk.PeekInt32(LocalsOffset);

        /// <summary>
        /// The size of the parameters, in DWORDs.
        /// </summary>
        public short Params => chunk.PeekInt16(ParamsOffset);

        /// <summary>
        /// The number of bytes in the function prolog code.
        /// </summary>
        public byte cbProlog => (byte) (flags & 0xFF);

        /// <summary>
        /// The number of registers saved.
        /// </summary>
        public byte cbRegs => (byte) ((flags >> 8) & 0x7);

        /// <summary>
        /// A variable that indicates whether the function uses structured exception handling.
        /// </summary>
        public bool fHasSEH => ((flags >> 11) & 0x1) == 1;

        /// <summary>
        /// A variable that indicates whether the EBP register has been allocated.
        /// </summary>
        public bool fUseBP => ((flags >> 12) & 0x1) == 1;

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public bool reserved => ((flags >> 13) & 0x1) == 1;

        /// <summary>
        /// A variable that indicates the frame type.
        /// </summary>
        public FrameType cbFrame => (FrameType) ((flags >> 14) & 0x3);

        private ushort flags => chunk.PeekUInt16(flagsOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //OffStart
            sizeof(int) + //ProcSize
            sizeof(int) + //Locals
            sizeof(short) + //Params
            sizeof(short); //Flags

        private readonly MemoryChunk chunk;

        internal FpoData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FPO_DATA, this, ViewKind.FpoData, StructSize);

        int IViewable.NumChildren() => 10;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("ulOffStart", OffStartOffset, OffStart);
                    break;

                case 1:
                    structWriter.WriteField("cbProcSize", ProcSizeOffset, ProcSize);
                    break;

                case 2:
                    structWriter.WriteField("cdwLocals", LocalsOffset, Locals);
                    break;

                case 3:
                    structWriter.WriteField("cdwParams", ParamsOffset, Params);
                    break;

                #region BitField

                case 4:
                    structWriter.WriteBitField(nameof(cbProlog), flagsOffset, cbProlog, sizeof(ushort), 8);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(cbRegs), flagsOffset, cbRegs, sizeof(ushort), 3);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(fHasSEH), flagsOffset, fHasSEH, sizeof(ushort), 1);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(fUseBP), flagsOffset, fUseBP, sizeof(ushort), 1);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(reserved), flagsOffset, reserved, sizeof(ushort), 1);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(cbFrame), flagsOffset, cbFrame, sizeof(ushort), 2);
                    break;

                #endregion

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return cbFrame.ToString();
        }
    }
}
