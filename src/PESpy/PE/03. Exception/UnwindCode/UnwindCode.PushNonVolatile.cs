using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy
{
    public readonly unsafe partial struct UnwindCode
    {
        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        public readonly struct PushNonVolatile : IViewable
        {
            internal string DebuggerDisplay() => $"[{UnwindOp}] CodeOffset = {CodeOffset}, OpInfo = {OpInfo}";

            private readonly byte* value;

            public byte CodeOffset => *value;

            /// <summary>
            /// <see cref="UWOP.UWOP_PUSH_NONVOL"/>
            /// </summary>
            public UWOP UnwindOp => (UWOP) (*(value + 1) & 0x0F);

            public UnwindInfo.Register OpInfo => (UnwindInfo.Register) ((*(value + 1) & 0xF0) >> 4);

            internal const int StructSize =
                sizeof(byte) + //CodeOffset
                sizeof(byte);  //UnwindOp + OpInfo

            public PushNonVolatile(byte* value)
            {
                this.value = value;
            }

            public static implicit operator byte*(PushNonVolatile value) => value.value;

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(this, ViewKind.UnwindCode, StructSize);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int IViewable.NumChildren() => 3;

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

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
