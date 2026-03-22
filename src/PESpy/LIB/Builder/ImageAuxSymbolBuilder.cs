namespace PESpy
{
    internal class ImageAuxSymbolBuilder
    {
        public byte[] Bytes { get; }

        internal ImageAuxSymbolBuilder(ImageAuxSymbol auxSymbol)
        {
            Bytes = auxSymbol.Bytes.ToArray();
        }

        public void WriteTo(FileWriter writer)
        {
            writer.WriteBytes(Bytes);
        }
    }
}
