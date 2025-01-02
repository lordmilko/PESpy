using System;

namespace PESpy
{
    public readonly struct ImageEpilogueDynamicRelocationHeader : IValue
    {
        public int EpilogueCount { get; }
        public byte EpilogueByteCount { get; }
        public byte BranchDescriptorElementSize { get; }
        public short BranchDescriptorCount { get; }

        public int Offset { get; }

        internal ImageEpilogueDynamicRelocationHeader(IFileReader reader)
        {
            Offset = (int) reader.Position;

            EpilogueCount = reader.ReadInt32();
            EpilogueByteCount = reader.ReadByte();
            BranchDescriptorElementSize = reader.ReadByte();
            BranchDescriptorCount = reader.ReadInt16();

            throw new NotImplementedException();
        }
    }
}
