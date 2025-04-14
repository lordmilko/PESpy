namespace PESpy
{
    public readonly struct ImageAuxSymbol : IValue
    {
        public int Offset { get; }

        internal ImageAuxSymbol(IFileReader reader)
        {
            Offset = (int) reader.Position;

            //IMAGE_AUX_SYMBOL has a number of unioned fields; don't know how to detect which one is in use
            reader.ReadBytes(18);
        }
    }
}
