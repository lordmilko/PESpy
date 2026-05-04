
namespace PESpy
{
    /// <summary>
    /// Represents a primitive value that was found in a <see cref="PEFile"/> that is not part of a larger data structure.
    /// </summary>
    public readonly struct RawValue<T> : IValue
    {
        public T Value { get; }

        public long Offset { get; }

        public RawValue(long offset, T value)
        {
            Offset = offset;
            Value = value;
        }

        public override string ToString()
        {
            return Value!.ToString();
        }
    }
}
