namespace PESpy
{
    internal class NullSymbolAccessor : ISymbolAccessor
    {
        public static readonly NullSymbolAccessor Instance = new NullSymbolAccessor();

        public SymbolAccessorKind Kind => SymbolAccessorKind.Null;

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            name = default;
            displacement = default;
            return false;
        }

        public bool TryGetAddressFromName(SymString name, out int targetAddress)
        {
            targetAddress = default;
            return false;
        }        

        public void Dispose()
        {
        }
    }
}
