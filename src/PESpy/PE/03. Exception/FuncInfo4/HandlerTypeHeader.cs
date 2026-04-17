namespace PESpy
{
    [Source(SourceKind.ehdata4_export_h)]
    public readonly struct HandlerTypeHeader
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

        public contType contAddr => (contType) (Value & 0b00110000);

        public byte unused => (byte) (Value & 0b11000000);

        public byte Value { get; }

        public int Offset { get; }

        internal const int StructSize = sizeof(byte);

        public HandlerTypeHeader(int offset, byte value)
        {
            Offset = offset;
            Value = value;
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
