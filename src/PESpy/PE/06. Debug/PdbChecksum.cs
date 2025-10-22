using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Doesn't seem to have a native representation, which might make sense considering it's explicitly designed
    //for managed assemblies
    public readonly struct PdbChecksum : IValue, IViewable
    {
        private const int AlgorithmNameOffset = 0;
        private int ChecksumOffset => AlgorithmName.Length + 1;

        public Utf8String AlgorithmName => chunk.PeekUtf8NullTerminatedString(AlgorithmNameOffset);

        public NativeSpan<byte> Checksum => chunk.PeekNativeSpan<byte>(ChecksumOffset, sizeOfData - (AlgorithmName.Length + 1));

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;
        private readonly int sizeOfData;

        internal PdbChecksum(in MemoryChunk chunk, int sizeOfData)
        {
            this.chunk = chunk;
            this.sizeOfData = sizeOfData;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.PdbChecksum, this, ViewKind.PdbChecksum, sizeOfData);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteUtf8NullTerminatedField(nameof(AlgorithmName), AlgorithmNameOffset, AlgorithmName);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Checksum), ChecksumOffset, Checksum);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return AlgorithmName.ToString();
        }
    }
}
