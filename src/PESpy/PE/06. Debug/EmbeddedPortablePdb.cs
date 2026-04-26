using System;
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
        private const int SignatureOffset = 0;
        private const int UncompressedSizeOffset = 4;
        private const int PortablePdbImageOffset = 8;

        public int Signature => chunk.PeekInt32(SignatureOffset);

        public int UncompressedSize => chunk.PeekInt32(UncompressedSizeOffset);

        public NativeSpan<byte> PortablePdbImage => chunk.PeekNativeSpan<byte>(PortablePdbImageOffset, sizeOfData - 8);

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
            writer.NewStruct(this, ViewKind.EmbeddedPortablePdb, sizeOfData);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Signature), SignatureOffset, Signature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(UncompressedSize), UncompressedSizeOffset, UncompressedSize);
                    break;

                case 2:
                    structWriter.WriteField(nameof(PortablePdbImage), PortablePdbImageOffset, PortablePdbImage);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
