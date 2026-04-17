namespace PESpy.Native
{
    internal struct FuncInfoHeader
    {
        /*union
        {
    #pragma warning(push)
    #pragma warning(disable: 4201) // nonstandard extension used: nameless struct/union
            struct
            {
                uint8_t isCatch        : 1;  // 1 if this represents a catch funclet, 0 otherwise
                uint8_t isSeparated    : 1;  // 1 if this function has separated code segments, 0 otherwise
                uint8_t BBT            : 1;  // Flags set by Basic Block Transformations
                uint8_t UnwindMap      : 1;  // Existence of Unwind Map RVA
                uint8_t TryBlockMap    : 1;  // Existence of Try Block Map RVA
                uint8_t EHs            : 1;  // EHs flag set
                uint8_t NoExcept       : 1;  // NoExcept flag set
                uint8_t reserved       : 1;
            };
    #pragma warning(pop)
        };*/
        public byte value;
    }
}
