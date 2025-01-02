using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    //Doesn't seem to have a native representation, which might make sense considering it's explicitly designed
    //for managed assemblies
    public readonly struct PdbChecksum : IValue, IViewable
    {
        public string AlgorithmName { get; init; }

        public byte[] Checksum { get; init; }

        public RawOffset Offset { get; }

        internal PdbChecksum(IFileReader reader, int size)
        {
            Offset = (RawOffset) reader.Position;

            AlgorithmName = reader.ReadUTF8NullTerminatedString();
            Checksum = reader.ReadBytes(size - (AlgorithmName.Length + 1));
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PdbChecksum), this, ViewKind.PdbChecksum);

            s.WriteUTF8NullTerminatedField(nameof(AlgorithmName), AlgorithmName);
            s.WriteField(nameof(Checksum), Checksum);
        }

        public override string ToString()
        {
            return AlgorithmName;
        }
    }
}
