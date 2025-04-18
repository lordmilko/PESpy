namespace PESpy.PDB
{
    /// <summary>
    /// Represents a 16-bit module index.
    /// </summary>
    public readonly struct IMOD
    {
        public const ushort Nil = ushort.MaxValue; //-1

        private readonly ushort value;

        public IMOD(ushort value)
        {
            this.value = value;
        }

        public static implicit operator ushort(IMOD value) => value.value;
        public static implicit operator IMOD(ushort value) => new IMOD(value);

        public override string ToString()
        {
            return value switch
            {
                Nil => "imodNil",
                _ => value.ToString()
            };
        }
    }
}
