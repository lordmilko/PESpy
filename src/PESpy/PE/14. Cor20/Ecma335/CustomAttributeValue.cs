namespace PESpy.Ecma335
{
    public readonly struct CustomAttributeValue<TType>
    {
        public CustomAttributeTypedArgument<TType>[] FixedArgs { get; }

        public CustomAttributeNamedArgument<TType>[] NamedArgs { get; }

        internal CustomAttributeValue(CustomAttributeTypedArgument<TType>[] fixedArgs, CustomAttributeNamedArgument<TType>[] namedArgs)
        {
            FixedArgs = fixedArgs;
            NamedArgs = namedArgs;
        }
    }
}
