using System.Diagnostics;

namespace PESpy
{
    public enum SymStoreKeyKind
    {
        PE,
        PDB,
    }

    [DebuggerDisplay("[{Kind}] {Value.ToString(),nq}")]
    public readonly struct SymStoreKey
    {
        public string Value { get; }

        public SymStoreKeyKind Kind { get; }

        public SymStoreKey(string value, SymStoreKeyKind kind)
        {
            Value = value;
            Kind = kind;
        }
    }
}
