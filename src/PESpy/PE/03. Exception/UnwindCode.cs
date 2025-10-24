using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="UNWIND_CODE"/> structure.
    /// Specific types of unwind codes are modelled via sub-classes of this class.
    /// </summary>
    [DebuggerDisplay("CodeOffset = {CodeOffset}, UnwindOp = {UnwindOp}")]
    public abstract class UnwindCode : IViewableValue
    {
        private const int CodeOffsetOffset = 0;
        private const int UnwindOpOffset = 1;

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
            writer.NewStruct(Strings.UNWIND_CODE, this, ViewKind.UnwindCode, StructSize);

        int IViewable.NumChildren() => NumChildren;

        protected virtual int NumChildren => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(CodeOffset), CodeOffsetOffset, CodeOffset);
                    break;

                #region BitField

                case 1:
                    structWriter.WriteBitField(nameof(UnwindOp), UnwindOpOffset, UnwindOp, sizeof(byte), 4);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(OpInfo), UnwindOpOffset, OpInfo, sizeof(byte), 4);
                    break;

                #endregion

                default:
                    WriteExtraChild(index, ref structWriter);
                    break;
            }
        }

        protected virtual void WriteExtraChild(int index, ref StructWriter structWriter)
        {
            throw new IndexOutOfRangeException();
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
            private const int SizeOffset = 2;

            public int Size { get; }

            public override int StructSize =>
                (OpInfo == 0 ? sizeof(short) : sizeof(int)) + //Size
                base.StructSize;

            protected override int NumChildren => 4;

            public AllocLarge(int offset, byte codeOffset, byte opInfo, int size) : base(offset, codeOffset, UWOP.ALLOC_LARGE)
            {
                OpInfo = opInfo;
                Size = size;
            }

            protected override void WriteExtraChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 3:
                        if (OpInfo == 0)
                            structWriter.WriteField(nameof(Size), SizeOffset, (ushort) Size);
                        else
                            structWriter.WriteField(nameof(Size), SizeOffset, Size); //SizeHi has been shifted 16 bits and added to SizeLo. We don't currently store them separately

                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
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
            private const int StackOffsetOffset = 2;

            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public ushort StackOffset { get; }

            public override int StructSize =>
                sizeof(short) + //StackOffset
                base.StructSize;

            protected override int NumChildren => 4;

            public SaveNonVolatile(int offset, byte codeOffset, UnwindInfo.X64Register register, ushort stackOffset) : base(offset, codeOffset, UWOP.SAVE_NONVOL)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            protected override void WriteExtraChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 3:
                        structWriter.WriteField(nameof(StackOffset), StackOffsetOffset, StackOffset);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public class SaveNonVolatileFar : UnwindCode
        {
            private const int StackOffsetOffset = 2;

            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public int StackOffset { get; }

            public override int StructSize =>
                sizeof(int) + //StackOffset
                base.StructSize;

            protected override int NumChildren => 4;

            public SaveNonVolatileFar(int offset, byte codeOffset, UnwindInfo.X64Register register, int stackOffset) : base(offset, codeOffset, UWOP.SAVE_NONVOL_FAR)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            protected override void WriteExtraChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 3:
                        //The high word has been shifted 16 bits to the right and then added to the low word.
                        //We don't currently store them separately
                        structWriter.WriteField(nameof(StackOffset), StackOffsetOffset, StackOffset);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
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
            private const int StackOffsetOffset = 2;

            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public ushort StackOffset { get; }

            public override int StructSize =>
                sizeof(ushort) + //StackOffset
                base.StructSize;

            protected override int NumChildren => 4;

            public SaveXmm128(int offset, byte codeOffset, UnwindInfo.X64Register register, ushort stackOffset) : base(offset, codeOffset, UWOP.SAVE_XMM128)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            protected override void WriteExtraChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 3:
                        structWriter.WriteField(nameof(StackOffset), StackOffsetOffset, StackOffset);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public class SaveXmm128Far : UnwindCode
        {
            private const int StackOffsetOffset = 2;

            public UnwindInfo.X64Register Register { get; }

            public override byte OpInfo => (byte) Register;

            public int StackOffset { get; }

            public override int StructSize =>
                sizeof(int) + //StackOffset
                base.StructSize;

            protected override int NumChildren => 4;

            public SaveXmm128Far(int offset, byte codeOffset, UnwindInfo.X64Register register, int stackOffset) : base(offset, codeOffset, UWOP.SAVE_XMM128_FAR)
            {
                Register = register;
                StackOffset = stackOffset;
            }

            protected override void WriteExtraChild(int index, ref StructWriter structWriter)
            {
                switch (index)
                {
                    case 3:
                        //The high word has been shifted 16 bits to the right and then added to the low word.
                        //We don't currently store them separately
                        structWriter.WriteField(nameof(StackOffset), StackOffsetOffset, StackOffset);
                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
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
