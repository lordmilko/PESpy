namespace PESpy
{
    public struct NDR64_PROC_FLAGS
    {
        public uint HandleType => (data >> 0) & 0x7;
        public uint ProcType => (data >> 3) & 0x7;
        public uint IsInterpreted => (data >> 6) & 0x3;
        public bool IsObject => (data & 100) != 0;
        public bool IsAsync => (data & 0x200) != 0;
        public bool IsEncode => (data & 0x400) != 0;
        public bool IsDecode => (data & 0x800) != 0;
        public bool UsesFullPtrPackage => (data & 0x1000) != 0;
        public bool UsesRpcSmPackage => (data & 0x2000) != 0;
        public bool UsesPipes => (data & 0x4000) != 0;
        public uint HandlesExceptions => (data >> 15) & 0x3;
        public bool ServerMustSize => (data & 0x20000) != 0;
        public bool ClientMustSize => (data & 0x40000) != 0;
        public bool HasReturn => (data & 0x80000) != 0;
        public bool HasComplexReturn => (data & 0x100000) != 0;
        public bool ServerHasCorrelation => (data & 0x200000) != 0;
        public bool ClientHasCorrelation => (data & 0x400000) != 0;
        public bool HasNotify => (data & 0x800000) != 0;
        public bool HasOtherExtensions => (data & 0x1000000) != 0;
        public uint Reserved => (data >> 25) & 0x7F;

        private uint data;

        public static implicit operator NDR64_PROC_FLAGS(uint value) => new NDR64_PROC_FLAGS { data = value };
    }
}
