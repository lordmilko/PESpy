namespace PESpy.PDB
{
    /// <summary>
    /// Describes a 32-bit page number within the PDB.<para/>
    /// In microsoft-pdb there are three types used to model page numbers: PN (16-bit), PN32 (32-bit) and Universal PN (32-bit). We
    /// are only modelling a 32-bit version
    /// </summary>
    /// <remarks>
    /// PDB files are divided into <see cref="BigMsfHdr.NumPages"/> pages, each <see cref="BigMsfHdr.PageSize"/> bytes large.
    /// There should be exactly <see cref="BigMsfHdr.NumPages"/> * <see cref="BigMsfHdr.PageSize"/> bytes in any given PDB file.
    /// </remarks>
    public readonly struct PN
    {
        private readonly uint value;

        public PN(uint value)
        {
            this.value = value;
        }

        public static implicit operator int(PN value) => (int) value.value;

        public static implicit operator PN(int value) => new PN((uint) value);

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
