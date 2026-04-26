using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy
{
    public readonly unsafe partial struct UnwindCode
    {
        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        public readonly struct AllocLarge : IViewable
        {
            private const int SizeOffset = 2;

            internal string DebuggerDisplay() => $"[{UnwindOp}] CodeOffset = {CodeOffset}, OpInfo = {OpInfo}, Size = {Size}";

            private readonly byte* value;

            public byte CodeOffset => *value;

            public UWOP UnwindOp => (UWOP) (*(value + 1) & 0x0F);

            public byte OpInfo => (byte) ((*(value + 1) & 0xF0) >> 4);

            /// <summary>
            /// If the operation info equals 0, then the size of the allocation divided by 8 is recorded in the next slot,
            /// allowing an allocation up to 512K - 8. If the operation info equals 1, then the unscaled size of the
            /// allocation is recorded in the next two slots in little-endian format, allowing allocations up to 4GB - 8.<para/>
            /// This property returns the scaled size (pre-multiplied by 8 when <see cref="OpInfo"/> is 0)
            /// </summary>
            public int Size
            {
                get
                {
                    //If the operation info equals 0, then the size of the allocation divided by 8 is recorded in the next slot,
                    //allowing an allocation up to 512K - 8
                    if (OpInfo == 0)
                    {
                        return *(ushort*) (value + SizeOffset) * 8;
                    }
                    else
                    {
                        //If the operation info equals 1, then the unscaled size of the allocation is recorded in the next two slots
                        //in little-endian format, allowing allocations up to 4GB - 8

                        var sizeLo = *(ushort*) (value + SizeOffset);
                        var sizeHi = *(ushort*) (value + SizeOffset + sizeof(short));

                        var size = sizeLo + (sizeHi << 16);

                        return size;
                    }
                }
            }

            internal int StructSize => OpInfo == 0 ? 4 : 6;

            public AllocLarge(byte* value)
            {
                this.value = value;
            }

            public static implicit operator byte*(AllocLarge value) => value.value;

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewUnmanagedStruct(this, ViewKind.UnwindCode, StructSize);

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
                        if (OpInfo == 0)
                            structWriter.WriteField(nameof(Size), SizeOffset, (ushort) Size / 8);
                        else
                            structWriter.WriteField(nameof(Size), SizeOffset, Size); //SizeHi has been shifted 16 bits and added to SizeLo. We don't currently store them separately

                        break;

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
