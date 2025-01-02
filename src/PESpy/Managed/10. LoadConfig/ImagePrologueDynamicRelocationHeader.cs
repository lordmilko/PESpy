using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImagePrologueDynamicRelocationHeader : IValue, IViewable
    {
        public int PrologueByteCount { get; }

        public byte[] PrologueBytes { get; }

        public int Offset { get; }

        internal ImagePrologueDynamicRelocationHeader(IFileReader reader)
        {
            Offset = (int) reader.Position;

            PrologueByteCount = reader.ReadByte();
            PrologueBytes = reader.ReadBytes(PrologueByteCount);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_PROLOGUE_DYNAMIC_RELOCATION_HEADER), this, ViewKind.ImagePrologueDynamicRelocationHeader);

            s.WriteField(nameof(PrologueByteCount), PrologueByteCount);
            s.WriteField(nameof(PrologueBytes), PrologueBytes);
        }
    }
}
