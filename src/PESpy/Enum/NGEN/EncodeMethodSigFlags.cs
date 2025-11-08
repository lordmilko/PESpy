namespace PESpy
{
    public enum EncodeMethodSigFlags
    {
        ENCODE_METHOD_SIG_UnboxingStub = 0x01,
        ENCODE_METHOD_SIG_InstantiatingStub = 0x02,
        ENCODE_METHOD_SIG_MethodInstantiation = 0x04,
        ENCODE_METHOD_SIG_SlotInsteadOfToken = 0x08,
        ENCODE_METHOD_SIG_MemberRefToken = 0x10,
        ENCODE_METHOD_SIG_Constrained = 0x20,
        ENCODE_METHOD_SIG_OwnerType = 0x40,
    }
}
