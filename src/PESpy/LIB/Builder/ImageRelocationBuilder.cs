namespace PESpy
{
    internal class ImageRelocationBuilder
    {
        public int VirtualAddress { get; set; }

        public int SymbolTableIndex {  get; set; }

        public short Type {  get; set; }

        internal ImageRelocationBuilder(ImageRelocation imageRelocation)
        {
            VirtualAddress = imageRelocation.VirtualAddress;
            SymbolTableIndex = imageRelocation.SymbolTableIndex;
            Type = imageRelocation.Type;
        }

        public void WriteTo(FileWriter writer)
        {
            writer.WriteUInt32((uint) VirtualAddress);
            writer.WriteUInt32((uint) SymbolTableIndex);
            writer.WriteUInt16((ushort) Type);
        }
    }
}
