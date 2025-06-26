using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
using static PESpy.View.ViewWriter;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="UNWIND_CODE"/> structure.
    /// Specific types of unwind codes are modelled via sub-classes of this class.
    /// </summary>
    [DebuggerDisplay("CodeOffset = {CodeOffset}, UnwindOp = {UnwindOp}")]
    public abstract class UnwindCode : IValue, IViewable
    {
        public int Offset { get; }

        public byte CodeOffset { get; }

        public UWOP UnwindOp { get; }

        public virtual byte OpInfo { get; protected set; }

        public virtual int StructSize =>
            sizeof(byte) + //CodeOffset
            sizeof(byte); //UnwindOp

        internal UnwindCode(int offset, byte codeOffset, UWOP unwindOp)
        {
            Offset = offset;
            CodeOffset = codeOffset;
            UnwindOp = unwindOp;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        //An UnwindCode is at least 2 bytes. Anyone who overrides WriteViewExtra is larger
        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(UNWIND_CODE), this, ViewKind.UnwindCode, StructSize);


        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            //Can't pass a using variable by ref
            var s = viewWriter.CreateStruct(parent);

            try
            {
                s.WriteField(nameof(CodeOffset), CodeOffset);

                using (var b = s.WriteBitFields<byte>())
                {
                    b.WriteField(nameof(UnwindOp), UnwindOp, 4);
                    b.WriteField(nameof(OpInfo), OpInfo, 4);
                }

                WriteViewExtra(ref s);

                return s.ToArray();
            }
            finally
            {
                s.Dispose();
            }
        }

        internal virtual void WriteViewExtra(ref StructWriter structWriter)
        {
        }

        public class PushNonVolatile : UnwindCode
        {
            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public PushNonVolatile(int offset, byte codeOffset, UnwindInfo.X64Register register) : base(offset, codeOffset, UWOP.PUSH_NONVOL)
            {
                Register = register;
            }
        }

        public class AllocLarge : UnwindCode
        {
            public int Size { get; }

            public override int StructSize =>
                (OpInfo == 0 ? sizeof(short) : sizeof(int)) + //Size
                base.StructSize;

            public AllocLarge(int offset, byte codeOffset, byte opInfo, int size) : base(offset, codeOffset, UWOP.ALLOC_LARGE)
            {
                OpInfo = opInfo;
                Size = size;
            }

            internal override void WriteViewExtra(ref StructWriter structWriter)
            {
                if (OpInfo == 0)
                    structWriter.WriteField(nameof(Size), (ushort) Size);
                else
                    structWriter.WriteField(nameof(Size), Size); //SizeHi has been shifted 16 bits and added to SizeLo. We don't currently store them separately
            }
        }

        public class AllocSmall : UnwindCode
        {
            public int Size { get; } //-8 and then /8 to get the original OpInfo

            public override byte OpInfo => (byte) ((Size - 8) / 8);

            public AllocSmall(int offset, byte codeOffset, int size) : base(offset, codeOffset, UWOP.ALLOC_SMALL)
            {
                Size = size;
            }
        }

        public class SetFpReg : UnwindCode
        {
            public UnwindInfo.X64Register FrameRegister { get; }

            public int FrameOffset { get; }

            public SetFpReg(int offset, byte codeOffset, UnwindInfo.X64Register frameRegister, byte opInfo, int frameOffset) : base(offset, codeOffset, UWOP.SET_FPREG)
            {
                FrameRegister = frameRegister;
                OpInfo = opInfo;
                FrameOffset = frameOffset;
            }
        }

        public class SaveNonVolatile : UnwindCode
        {
            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public ushort StackOffset { get; }

            public override int StructSize =>
                sizeof(short) + //StackOffset
                base.StructSize;

            public SaveNonVolatile(int offset, byte codeOffset, UnwindInfo.X64Register register, ushort stackOffset) : base(offset, codeOffset, UWOP.SAVE_NONVOL)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            internal override void WriteViewExtra(ref StructWriter structWriter)
            {
                structWriter.WriteField(nameof(StackOffset), StackOffset);
            }
        }

        public class SaveNonVolatileFar : UnwindCode
        {
            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public int StackOffset { get; }

            public override int StructSize =>
                sizeof(int) + //StackOffset
                base.StructSize;

            public SaveNonVolatileFar(int offset, byte codeOffset, UnwindInfo.X64Register register, int stackOffset) : base(offset, codeOffset, UWOP.SAVE_NONVOL_FAR)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            internal override void WriteViewExtra(ref StructWriter structWriter)
            {
                //The high word has been shifted 16 bits to the right and then added to the low word.
                //We don't currently store them separately
                structWriter.WriteField(nameof(StackOffset), StackOffset);
            }
        }

        public class Epilog : UnwindCode
        {
            public Epilog(int offset, byte codeOffset, byte opInfo) : base(offset, codeOffset, UWOP.UWOP_EPILOG)
            {
                OpInfo = opInfo;
            }
        }

        public class SaveXmm128 : UnwindCode
        {
            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public ushort StackOffset { get; }

            public override int StructSize =>
                sizeof(int) + //StackOffset
                base.StructSize;

            public SaveXmm128(int offset, byte codeOffset, UnwindInfo.X64Register register, ushort stackOffset) : base(offset, codeOffset, UWOP.SAVE_XMM128)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            internal override void WriteViewExtra(ref StructWriter structWriter)
            {
                structWriter.WriteField(nameof(StackOffset), StackOffset);
            }
        }

        public class SaveXmm128Far : UnwindCode
        {
            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public int StackOffset { get; }

            public override int StructSize =>
                sizeof(int) + //StackOffset
                base.StructSize;

            public SaveXmm128Far(int offset, byte codeOffset, UnwindInfo.X64Register register, int stackOffset) : base(offset, codeOffset, UWOP.SAVE_XMM128_FAR)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            internal override void WriteViewExtra(ref StructWriter structWriter)
            {
                //The high word has been shifted 16 bits to the right and then added to the low word.
                //We don't currently store them separately
                structWriter.WriteField(nameof(StackOffset), StackOffset);
            }
        }

        public class PushMachFrame : UnwindCode
        {
            public PushMachFrame(int offset, byte codeOffset, byte opInfo) : base(offset, codeOffset, UWOP.PUSH_MACHFRAME)
            {
                OpInfo = opInfo;
            }
        }

        public class NullUnwindCode : UnwindCode
        {
            public NullUnwindCode(int offset, byte codeOffset, UWOP unwindOp, byte opInfo) : base(offset, codeOffset, unwindOp)
            {
                OpInfo = opInfo;
            }
        }
    }
}
