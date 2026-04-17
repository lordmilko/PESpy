using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy
{
    public readonly unsafe partial struct UnwindCode
    {
        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        public readonly struct SetFpReg : IViewable
        {
            internal string DebuggerDisplay() => $"[{UnwindOp}] CodeOffset = {CodeOffset}, OpInfo = {OpInfo}, FrameRegister = {FrameRegister}, FrameOffset = {FrameOffset}";

            private readonly byte* value;
            private readonly byte* pUnwindInfo; //Needed to get the offset and register in SetFpReg

            public byte CodeOffset => *value;

            public UWOP UnwindOp => (UWOP) (*(value + 1) & 0x0F);

            /// <summary>
            /// The operation info field is reserved and shouldn't be used.
            /// </summary>
            public byte OpInfo => (byte) ((*(value + 1) & 0xF0) >> 4);

            public UnwindInfo.Register FrameRegister => (UnwindInfo.Register) (*(pUnwindInfo + UnwindInfo.frameRegisterAndOffsetOffset) & 0x0F);

            /// <summary>
            /// The offset is equal to the Frame Register offset (scaled) field in the UNWIND_INFO * 16, allowing offsets from 0 to 240.
            /// The use of an offset permits establishing a frame pointer that points to the middle of the fixed stack allocation,
            /// helping code density by allowing more accesses to use short instruction forms.<para/>
            /// This property returns the scaled size offset (pre-multiplied by 16).
            /// </summary>
            public byte FrameOffset => (byte) (((*(pUnwindInfo + UnwindInfo.frameRegisterAndOffsetOffset) & 0xF0) >> 4) * 16);

            internal const int StructSize =
                sizeof(byte) + //CodeOffset
                sizeof(byte);  //UnwindOp + OpInfo

            public SetFpReg(byte* pUnwindCode, byte* pUnwindInfo)
            {
                this.value = pUnwindCode;
                this.pUnwindInfo = pUnwindInfo;
            }

            public static implicit operator byte*(SetFpReg value) => value.value;

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(Strings.UNWIND_CODE, this, ViewKind.UnwindCode, StructSize);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int IViewable.NumChildren() => 3; //The FrameRegister and FrameOffset aren't members of the UNWIND_CODE

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IViewable.WriteChild(int index, ref StructWriter structWriter)
            {
                //The FrameRegister and FrameOffset aren't members of the UNWIND_CODE

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
