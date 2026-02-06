namespace PESpy.UI
{
    struct EnumFlagInfo
    {
        public ulong Value;
        public string? Text;
        public int NumChars; //Every value will have the same NumChars, we just want to avoid an extra allocation by boxing an outer type; our only allocation is the EnumFlag[]

        internal EnumFlagInfo(ulong value, string? text, int numChars)
        {
            Value = value;
            Text = text;
            NumChars = numChars;
        }
    }
}
