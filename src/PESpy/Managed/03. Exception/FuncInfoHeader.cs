namespace PESpy
{
    public readonly struct FuncInfoHeader
    {
        /// <summary>
        /// 1 if this represents a catch funclet, 0 otherwise
        /// </summary>
        public bool IsCatch => (Value & 0b00000001) != 0;

        /// <summary>
        /// 1 if this function has separated code segments, 0 otherwise
        /// </summary>
        public bool IsSeparated => (Value & 0b00000010) != 0;

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

        public bool Reserved => (Value & 0b10000000) != 0;

        public byte Value { get; }

        public FuncInfoHeader(byte value)
        {
            Value = value;
        }
    }
}
