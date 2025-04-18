namespace PESpy.PDB
{
    public struct OMFSegMapFlags
    {
        public bool fRead => (fAll & (1 << 0)) != 0;
        public bool fWrite => (fAll & (1 << 1)) != 0;
        public bool fExecute => (fAll & (1 << 2)) != 0;
        public bool f32Bit => (fAll & (1 << 3)) != 0;
        public byte res1 => (byte) ((fAll >> 4) & 0b1111);
        public bool fSel => (fAll & (1 << 8)) != 0;
        public bool fAbs => (fAll & (1 << 9)) != 0;
        public byte res2 => (byte) ((fAll >> 10) & 0b11);
        public bool fGroup => (fAll & (1 << 12)) != 0;
        public byte res3 => (byte) ((fAll >> 13) & 0b111);

        private ushort fAll;

        public OMFSegMapFlags(ushort fAll)
        {
            this.fAll = fAll;
        }

        public static implicit operator OMFSegMapFlags(ushort value) => new OMFSegMapFlags(value);
    }
}
