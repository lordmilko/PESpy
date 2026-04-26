using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PESpy.View;

namespace PESpy
{
    public readonly unsafe partial struct UnwindCode
    {
        [DebuggerDisplay("{DebuggerDisplay(),nq}")]
        public readonly struct Epilog : IViewable
        {
            internal string DebuggerDisplay() => $"[{UnwindOp}] CodeOffset = {CodeOffset}, OpInfo = {OpInfo}";

            private readonly byte* value;

            public byte CodeOffset => *value;

            public UWOP UnwindOp => (UWOP) (*(value + 1) & 0x0F);

            public byte OpInfo => (byte) ((*(value + 1) & 0xF0) >> 4);

            internal const int StructSize =
                sizeof(byte) + //CodeOffset
                sizeof(byte);  //UnwindOp + OpInfo

            public Epilog(byte* pUnwindCode, byte* pUnwindInfo)
            {
                var version = (*pUnwindInfo) & 0x7; //bottom 3 bits

                //Contrary to what https://www.winehq.org/pipermail/wine-devel/2019-August/149669.html says,
                //regardless of whether opInfo was 0 or 1 it didn't seem like there was another slot after this one
                //that needed to be read
                if (version != 1 && version != 2)
                    throw new InvalidOperationException($"Don't know how to handle UWOP_EPILOG when using version {version}");

                this.value = pUnwindCode;
            }

            public static implicit operator byte*(Epilog value) => value.value;

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
