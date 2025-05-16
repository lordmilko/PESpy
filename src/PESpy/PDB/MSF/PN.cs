using System;

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
    public readonly struct PN : IEquatable<PN>
    {
        private readonly uint value;

        public PN(uint value)
        {
            this.value = value;
        }

        public static implicit operator int(PN value) => (int) value.value;

        public static implicit operator PN(int value) => new PN((uint) value);

        public override bool Equals(object obj)
        {
            if (obj is PN p)
                return p.value.Equals(p.value);

            if (obj is int i)
                return value == (uint) i;

            if (obj is uint u)
                return value == u;

            return false;
        }

        public bool Equals(PN other) => value == other.value;

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
