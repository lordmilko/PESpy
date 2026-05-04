using System;
using PESpy.View;

namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct HandlerTypeHeader : IViewableValue
    {
        /// <summary>
        /// Existence of Handler Type adjectives (bitfield)
        /// </summary>
        public bool adjectives => (Value & 0b00000001) != 0;

        /// <summary>
        /// Existence of Image relative offset of the corresponding type descriptor
        /// </summary>
        public bool dispType => (Value & 0b00000010) != 0;

        /// <summary>
        /// Existence of Displacement of catch object from base
        /// </summary>
        public bool dispCatchObj => (Value & 0b00000100) != 0;

        /// <summary>
        /// Continuation addresses are RVAs rather than function relative, used for separated code
        /// </summary>
        public bool contIsRVA => (Value & 0b00001000) != 0;

        public contType contAddr => (contType) ((Value >> 4) & 3);

        public byte unused => (byte) (Value & 0b11000000);

        public byte Value { get; }

        public long Offset { get; }

        internal const int StructSize = sizeof(byte);

        public HandlerTypeHeader(long offset, byte value)
        {
            Offset = offset;
            Value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.HandlerTypeHeader, StructSize);

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField(nameof(adjectives), 0, adjectives, 1, 1);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(dispType), 0, dispType, 1, 1);
                    break;

                case 2:
                    structWriter.WriteBitField(nameof(dispCatchObj), 0, dispCatchObj, 1, 1);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(contIsRVA), 0, contIsRVA, 1, 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(contAddr), 0, contAddr, 1, 2);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(unused), 0, unused, 1, 2);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public enum contType : byte
        {
            /// <summary>
            /// no continuation address in metadata, use what the catch funclet returns
            /// </summary>
            NONE = 0,

            /// <summary>
            /// one function-relative continuation address
            /// </summary>
            ONE = 1,

            /// <summary>
            /// two function-relative continuation addresses
            /// </summary>
            TWO = 2,

            /// <summary>
            /// reserved
            /// </summary>
            RESERVED = 3
        }
    }
}
