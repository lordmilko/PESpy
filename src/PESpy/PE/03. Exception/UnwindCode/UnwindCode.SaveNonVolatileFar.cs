using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy
{
    public readonly unsafe partial struct UnwindCode
    {
        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        public readonly struct SaveNonVolatileFar : IViewable
        {
            internal string DebuggerDisplay() => $"[{UnwindOp}] CodeOffset = {CodeOffset}, OpInfo = {OpInfo}, StackOffset = {StackOffset}";

            private const int StackOffsetOffset = 2;

            private readonly byte* value;

            public byte CodeOffset => *value;

            public UWOP UnwindOp => (UWOP) (*(value + 1) & 0x0F);

            public UnwindInfo.Register OpInfo => (UnwindInfo.Register) ((*(value + 1) & 0xF0) >> 4);

            public int StackOffset => (*(ushort*) (value + 2)) + (*(ushort*) (value + 4) << 16);

            internal const int StructSize =
                sizeof(byte) + //CodeOffset
                sizeof(byte) + //UnwindOp + OpInfo
                sizeof(int);   //StackOffset

            public SaveNonVolatileFar(byte* value)
            {
                this.value = value;
            }

            public static implicit operator byte*(SaveNonVolatileFar value) => value.value;

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(Strings.UNWIND_CODE, this, ViewKind.UnwindCode, StructSize);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int IViewable.NumChildren() => 4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    }
}
