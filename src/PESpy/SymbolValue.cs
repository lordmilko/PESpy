using PESpy.PDB;

namespace PESpy
{
    /// <summary>
    /// Encapsulates a simple value identified by a debug symbol.
    /// </summary>
    /// <typeparam name="T">The type of value that this type encapsulates.</typeparam>
    public readonly struct SymbolValue<T>
    {
        public long Offset { get; }

        public SymType SymType { get; }

        public T Value { get; }

        public int Length { get; }

        internal SymbolValue(long offset, SymType symType, T value, int length)
        {
            Offset = offset;
            SymType = symType;
            Value = value;
            Length = length;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
