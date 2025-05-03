using System;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents a Portable PDB that has been embedded in a Portable Executable file.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public class EmbeddedPortablePdb : IValue, IViewable //It's going to be boxed anyway
    {
        public const int MPDBSignature = 0x4244504d; //MPDB (i.e. "eMbedded PDB")

#if PEFAST
        public int Signature => chunk.PeekInt32(0);
#else
        public int Signature { get; }
#endif

#if PEFAST
        public int UncompressedSize => chunk.PeekInt32(4);
#else
        public int UncompressedSize { get; }
#endif

#if PEFAST
        public Span<byte> PortablePdbImage => chunk.PeekSpan<byte>(8, sizeOfData - 8);
#else
        public byte[] PortablePdbImage { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;
        private readonly int sizeOfData;

        internal EmbeddedPortablePdb(in MemoryChunk chunk, int sizeOfData)
        {
            this.chunk = chunk;
            this.sizeOfData = sizeOfData;
        }
#else
        internal EmbeddedPortablePdb(IFileReader reader, int sizeOfData)
        {
            Offset = (int) reader.Position;

            Signature = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            PortablePdbImage = reader.ReadBytes(sizeOfData - 8);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Embedded Portable PDB", this, ViewKind.EmbeddedPortablePdb);

            s.WriteField(nameof(Signature), Signature);
            s.WriteField(nameof(UncompressedSize), UncompressedSize);
            s.WriteField(nameof(PortablePdbImage), PortablePdbImage);
        }
    }
}
