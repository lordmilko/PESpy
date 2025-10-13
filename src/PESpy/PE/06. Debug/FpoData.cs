using System.ComponentModel;
using System.Diagnostics;
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
        /// <summary>
        /// The offset of the first byte of the function code.
        /// </summary>
        public int OffStart => chunk.PeekInt32(0);

        /// <summary>
        /// The number of bytes in the function.
        /// </summary>
        public int ProcSize => chunk.PeekInt32(4);

        /// <summary>
        /// The number of local variables.
        /// </summary>
        public int Locals => chunk.PeekInt32(8);

        /// <summary>
        /// The size of the parameters, in DWORDs.
        /// </summary>
        public short Params => chunk.PeekInt16(12);

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

        private ushort flags => chunk.PeekUInt16(14);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("ulOffStart", OffStart);
            s.WriteField("cbProcSize", ProcSize);
            s.WriteField("cdwLocals", Locals);
            s.WriteField("cdwParams", Params);

            using (var bitField = s.WriteBitFields<ushort>())
            {
                bitField.WriteField(nameof(cbProlog), cbProlog, 8);
                bitField.WriteField(nameof(cbRegs), cbRegs, 3);
                bitField.WriteField(nameof(fHasSEH), fHasSEH, 1);
                bitField.WriteField(nameof(fUseBP), fUseBP, 1);
                bitField.WriteField(nameof(reserved), reserved, 1);
                bitField.WriteField(nameof(cbFrame), cbFrame, 2);
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return cbFrame.ToString();
        }
    }
}
