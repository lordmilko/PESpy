namespace PESpy.PDB
{
    /// <summary>
    /// Represents a 16-bit stream number.
    /// </summary>
    public readonly struct SN
    {
        public const ushort UserMin = 1;
        public const ushort Max = 0x1000;

        public const ushort Nil = ushort.MaxValue; //-1
        public const ushort ST = 0;
        public const ushort PDB = 1;
        public const ushort TPI = 2;
        public const ushort DBI = 3;
        public const ushort IPI = 4;

        private readonly ushort value;

        public SN(ushort value)
        {
            this.value = value;
        }

        public static implicit operator ushort(SN value) => value.value;
        public static implicit operator SN(ushort value) => new SN(value);

        public override string ToString()
        {
            return value switch
            {
                Nil => "snNil",
                ST => "snSt",
                PDB => "snPDB",
                TPI => "snTpi",
                DBI => "snDbi",
                IPI => "snIpi",
                _ => value.ToString()
            };
        }
    }
}
