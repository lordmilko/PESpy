using System;

namespace PESpy.PDB
{
    //What size should SN be? It's 16-bit, however there's also UNSN which is the 32-bit version.
    //However, MSF::ReplaceStream takes the 16-bit SN, so it's not clear to me what would actually
    //happen if you tried to work with more than 65536 streams in both PESpy and microsoft-pdb

    /// <summary>
    /// Represents a 16-bit stream number.
    /// </summary>
    public readonly struct SN : IEquatable<SN>
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

        public override int GetHashCode() => value.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is SN sn)
                return value == sn.value;

            if (obj is short s)
                return (ushort) s == value;

            if (obj is ushort u)
                return u == value;

            if (obj is int i)
                return i == value;

            return false;
        }

        public bool Equals(SN other) => value == other.value;

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
