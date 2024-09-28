using PESpy.View;

namespace PESpy
{
    public readonly struct TypeDescriptor : IValue, IViewable
    {
        public ulong pVFTable { get; }

        public ulong Spare { get; }

        public string Name { get; }

        public int Offset { get; }

        internal TypeDescriptor(ref FileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            pVFTable = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadUInt64();
            Spare = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadUInt64();
            Name = reader.ReadAnsiNullTerminatedString();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.TypeDescriptor), this, ViewKind.TypeDescriptor);

            s.WritePointerField("pVFTable", pVFTable);
            s.WritePointerField("spare", Spare);
            s.WriteAnsiNullTerminatedField("name", Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
