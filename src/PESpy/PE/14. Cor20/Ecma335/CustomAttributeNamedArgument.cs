using System.Diagnostics;
using ClrDebug;

namespace PESpy.Ecma335
{
    [DebuggerDisplay("{Name,nq} = {Value}")]
    public readonly struct CustomAttributeNamedArgument<TType>
    {
        public FixedUtf8String Name { get; }

        public CorSerializationType Kind { get; }

        public TType Type { get; }

        public object? Value { get; }

        internal CustomAttributeNamedArgument(FixedUtf8String name, CorSerializationType kind, TType type, object? value)
        {
            Name = name;
            Kind = kind;
            Type = type;
            Value = value;
        }
    }
}
