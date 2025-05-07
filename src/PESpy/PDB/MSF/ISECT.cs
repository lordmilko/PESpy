namespace PESpy.PDB
{
    /// <summary>
    /// Represents a 16-bit section index.
    /// </summary>
    public readonly struct ISECT
    {
        public const ushort Nil = ushort.MaxValue; //-1

        private readonly ushort value;

        public ISECT(ushort value)
        {
            this.value = value;
        }

        public static implicit operator ushort(ISECT value) => value.value;
        public static implicit operator ISECT(ushort value) => new ISECT(value);
        public static implicit operator ISECT(short value) => new ISECT((ushort) value);

        public override string ToString()
        {
            return value switch
            {
                Nil => "isectNil",
                _ => value.ToString()
            };
        }
    }
}
