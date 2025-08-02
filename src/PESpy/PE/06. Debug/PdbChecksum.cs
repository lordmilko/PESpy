using System;
using PESpy.View;

namespace PESpy
{
    //Doesn't seem to have a native representation, which might make sense considering it's explicitly designed
    //for managed assemblies
    public readonly struct PdbChecksum : IValue, IViewable
    {
        public Utf8String AlgorithmName => chunk.PeekUtf8NullTerminatedString(0);

        public NativeSpan<byte> Checksum => chunk.PeekNativeSpan<byte>((AlgorithmName.Length + 1), sizeOfData - (AlgorithmName.Length + 1));

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteUTF8NullTerminatedField(nameof(AlgorithmName), AlgorithmName);
            s.WriteField(nameof(Checksum), Checksum);

            return s.ToArray();
        }

        public override string ToString()
        {
            return AlgorithmName.ToString();
        }
    }
}
