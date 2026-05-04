using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("[{Ordinal.ToString(\"X\"),nq}] {Name.ToString(),nq}")]
    public readonly struct NameAndOrdinal
    {
        public SymString Name { get; }

        //Ordinal into the EntryTable
        public short Ordinal { get; }

        internal NameAndOrdinal(SymString name, short ordinal)
        {
            Name = name;
            Ordinal = ordinal;
        }
    }
}
