using System.Diagnostics;
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

        public int Signature => chunk.PeekInt32(0);

        public int UncompressedSize => chunk.PeekInt32(4);

        public NativeSpan<byte> PortablePdbImage => chunk.PeekNativeSpan<byte>(8, sizeOfData - 8);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int sizeOfData;

        internal EmbeddedPortablePdb(in MemoryChunk chunk, int sizeOfData)
        {
            this.chunk = chunk;
            this.sizeOfData = sizeOfData;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.EmbeddedPortablePDB, this, ViewKind.EmbeddedPortablePdb, sizeOfData);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Signature), Signature);
            s.WriteField(nameof(UncompressedSize), UncompressedSize);
            s.WriteField(nameof(PortablePdbImage), PortablePdbImage);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
