using System.Diagnostics;
using ClrDebug;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{Name,nq} = {Value}")]
    public readonly struct CustomAttributeNamedArgument<TType>
    {
        public string Name { get; }

        public CorSerializationType Kind { get; }

        public TType Type { get; }

        public object? Value { get; }

        internal CustomAttributeNamedArgument(string name, CorSerializationType kind, TType type, object? value)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Value = value;
        }
    }
}
