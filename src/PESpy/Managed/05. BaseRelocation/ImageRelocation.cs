namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_RELOCATION"/> structure.
    /// </summary>
    public readonly struct ImageRelocation : IValue
    {
        public int VirtualAddress { get; init; }

        public int RelocCount => VirtualAddress;

        public int SymbolTableIndex { get; }

        public short Type { get; }

        public int Offset { get; }

        internal ImageRelocation(IFileReader reader)
        {
            Offset = (int) reader.Position;

            VirtualAddress = reader.ReadInt32();
            SymbolTableIndex = reader.ReadInt32();
            Type = reader.ReadInt16();
        }
    }
}
