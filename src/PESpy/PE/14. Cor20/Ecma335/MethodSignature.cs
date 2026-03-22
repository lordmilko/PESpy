using ClrDebug;

namespace PESpy.Ecma335
{
    public readonly struct MethodSignature<TType>
    {
        public CorCallingConvention CallingConvention { get; }

        public TType ReturnType { get; }

        public int RequiredParameterCount { get; }

        public int GenericParameterCount { get; }

        public TType[] ParameterTypes { get; }

        internal MethodSignature(CorCallingConvention callingConvention, TType returnType, int requiredParameterCount, int genericParameterCount, TType[] parameterTypes)
        {
            CallingConvention = callingConvention;
            ReturnType = returnType;
            GenericParameterCount = genericParameterCount;
            RequiredParameterCount = requiredParameterCount;
            ParameterTypes = parameterTypes;
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            builder.Append(ReturnType.ToString());
            builder.Append(" M(");

            for (var i = 0; i < ParameterTypes.Length; i++)
            {
                builder.Append(ParameterTypes[i].ToString());

                if (i < ParameterTypes.Length - 1)
                    builder.Append(", ");
            }

            builder.Append(')');

            return builder.ToString();
        }
    }
}
