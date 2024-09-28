using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a Portable PDB that has been embedded in a Portable Executable file.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct EmbeddedPortablePdb : IValue, IViewable
    {
        public const int MPDBSignature = 0x4244504d; //MPDB (i.e. "eMbedded PDB")

        public int Signature { get; }

        public int UncompressedSize { get; }

        public byte[] PortablePdbImage { get; }

        public int Offset { get; }

        public EmbeddedPortablePdb(ref FileReader reader, int sizeOfData)
        {
            Offset = (int) reader.Position;

            Signature = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            PortablePdbImage = reader.ReadBytes(sizeOfData - 8);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Embedded Portable PDB", this, ViewKind.EmbeddedPortablePdb);

            s.WriteField(nameof(Signature), Signature);
            s.WriteField(nameof(UncompressedSize), UncompressedSize);
            s.WriteField(nameof(PortablePdbImage), PortablePdbImage);
        }
    }
}
