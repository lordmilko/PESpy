using System;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct FuncInfoHeader : IViewableValue
    {
        /// <summary>
        /// 1 if this represents a catch funclet, 0 otherwise
        /// </summary>
        public bool isCatch => (Value & 0b00000001) != 0;

        /// <summary>
        /// 1 if this function has separated code segments, 0 otherwise
        /// </summary>
        public bool isSeparated => (Value & 0b00000010) != 0;

        /// <summary>
        /// Flags set by Basic Block Transformations
        /// </summary>
        public bool BBT => (Value & 0b00000100) != 0;

        /// <summary>
        /// Existence of Unwind Map RVA
        /// </summary>
        public bool UnwindMap => (Value & 0b00001000) != 0;

        /// <summary>
        /// Existence of Try Block Map RVA
        /// </summary>
        public bool TryBlockMap => (Value & 0b00010000) != 0;

        /// <summary>
        /// EHs flag set
        /// </summary>
        public bool EHs => (Value & 0b00100000) != 0;

        /// <summary>
        /// NoExcept flag set
        /// </summary>
        public bool NoExcept => (Value & 0b01000000) != 0;

        public byte reserved => (byte) (Value & 0b10000000);

        public byte Value { get; }

        public int Offset { get; }

        internal const int StructSize = sizeof(byte);

        public FuncInfoHeader(int offset, byte value)
        {
            Offset = offset;
            Value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.FuncInfoHeader, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField(nameof(isCatch), 0, isCatch, 1, 1);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(isSeparated), 0, isSeparated, 1, 1);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(BBT), 0, BBT, 1, 1);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(UnwindMap), 0, UnwindMap, 1, 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(TryBlockMap), 0, TryBlockMap, 1, 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(EHs), 0, EHs, 1, 1);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(NoExcept), 0, NoExcept, 1, 1);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(reserved), 0, reserved, 1, 1);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
