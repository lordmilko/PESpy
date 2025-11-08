namespace PESpy
{
    [Source(SourceKind.corcompile_h)]
    public readonly struct VirtualSectionData
    {
        private readonly ZapVirtualSectionType sectionType;

        public ZapVirtualSectionType IBCType => (ZapVirtualSectionType) (((uint) sectionType & ZapVirtualSectionTypeFlags.IBCTypeReservedFlag) >> 24);

        public ZapVirtualSectionType RangeType => (ZapVirtualSectionType) (((uint) sectionType & ZapVirtualSectionTypeFlags.RangeTypeReservedFlag) >> 16);

        public ZapVirtualSectionType VirtualSectionType => (ZapVirtualSectionType) (((uint) sectionType & ZapVirtualSectionTypeFlags.VirtualSectionTypeReservedFlag));

        public bool IsIBCProfiledColdSection() =>
            ((sectionType & ZapVirtualSectionType.ColdRange) == ZapVirtualSectionType.ColdRange) && ((sectionType & ZapVirtualSectionType.IBCProfiledSection) == ZapVirtualSectionType.IBCProfiledSection);

        internal VirtualSectionData(ZapVirtualSectionType sectionType)
        {
            this.sectionType = sectionType;
        }

        public static implicit operator VirtualSectionData(uint value) => new VirtualSectionData((ZapVirtualSectionType) value);

        public override string ToString()
        {
            return sectionType.ToString();
        }
    }
}
