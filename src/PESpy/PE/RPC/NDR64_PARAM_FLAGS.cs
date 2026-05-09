namespace PESpy
{
    public struct NDR64_PARAM_FLAGS
    {
        public bool MustSize => (data & 0x0001) != 0;
        public bool MustFree => (data & 0x0002) != 0;
        public bool IsPipe => (data & 0x0004) != 0;
        public bool IsIn => (data & 0x0008) != 0;
        public bool IsOut => (data & 0x0010) != 0;
        public bool IsReturn => (data & 0x0020) != 0;
        public bool IsBasetype => (data & 0x0040) != 0;
        public bool IsByValue => (data & 0x0080) != 0;
        public bool IsSimpleRef => (data & 0x0100) != 0;
        public bool IsDontCallFreeInst => (data & 0x0200) != 0;
        public bool SaveForAsyncFinish => (data & 0x0400) != 0;
        public bool IsPartialIgnore => (data & 0x0800) != 0;
        public bool IsForceAllocate => (data & 0x1000) != 0;
        public ushort Reserved => (ushort) ((data >> 13) & 0x3);
        public bool UseCache => (data & 0x8000) != 0;

        private ushort data;

        public static implicit operator NDR64_PARAM_FLAGS(ushort value) => new NDR64_PARAM_FLAGS { data = value };
    }
}
