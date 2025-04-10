namespace PESpy
{
    public readonly struct ImageAuxSymbol : IValue
    {
        public int Offset { get; }

        internal ImageAuxSymbol(IFileReader reader)
        {
            Offset = (int) reader.Position;

            reader.ReadBytes(18);
        }
    }
}
