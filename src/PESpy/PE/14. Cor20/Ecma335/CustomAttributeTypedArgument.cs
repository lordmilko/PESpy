using System.Diagnostics;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{Value}")]
    public readonly struct CustomAttributeTypedArgument<TType>
    {
        public TType Type { get; }
        public object? Value { get; }

        internal CustomAttributeTypedArgument(TType type, object? value)
        {
            Type = type;
            Value = value;
        }
    }
}
