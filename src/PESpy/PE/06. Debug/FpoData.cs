using System.ComponentModel;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
#if PEFAST
        public int OffStart => chunk.PeekInt32(0);
#else
        public int OffStart { get; }
#endif

        /// <summary>
        /// The number of bytes in the function.
        /// </summary>
#if PEFAST
        public int ProcSize => chunk.PeekInt32(4);
#else
        public int ProcSize { get; }
#endif

        /// <summary>
        /// The number of local variables.
        /// </summary>
#if PEFAST
        public int Locals => chunk.PeekInt32(8);
#else
        public int Locals { get; }
#endif

        /// <summary>
        /// The size of the parameters, in DWORDs.
        /// </summary>
#if PEFAST
        public short Params => chunk.PeekInt16(12);
#else
        public short Params { get; }
#endif

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

#if PEFAST
        private ushort flags => chunk.PeekUInt16(14);
#else
        private readonly ushort flags;
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //OffStart
            sizeof(int) + //ProcSize
            sizeof(int) + //Locals
            sizeof(short) + //Params
            sizeof(short); //Flags

#if PEFAST
        private readonly MemoryChunk chunk;

        internal FpoData(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal FpoData(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            OffStart = reader.ReadInt32();
            ProcSize = reader.ReadInt32();
            Locals = reader.ReadInt32();
            Params = reader.ReadInt16();
            flags = reader.ReadUInt16();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(FPO_DATA), this, ViewKind.FpoData, StructSize);

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

            return s.ToArray();
        }

        public override string ToString()
        {
            return cbFrame.ToString();
        }
    }
}
