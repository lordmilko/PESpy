using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct GlobalValueEntry : IValue, IViewable
    {
        public int Offset { get; }

        public VA<string> Name { get; }
        public long Address { get; }

        internal GlobalValueEntry(IFileReader reader, PEFile peFile, bool is32Bit)
        {
            Offset = (int) reader.Position;

            var name = is32Bit ? reader.ReadUInt32() : reader.ReadInt64();
            Address = is32Bit ? reader.ReadUInt32() : reader.ReadInt64();

            var oldPosition = reader.Position;

            Debug.Assert(peFile.IsLoadedImage);

            if (name != 0)
            {
                var actualOffset = (int) (name - peFile.OptionalHeader.ImageBase);
                reader.Seek(actualOffset);
                Name = new VA<string>(name, actualOffset, reader.ReadAnsiNullTerminatedString());
            }
            else
                Name = new VA<string>(name);

            reader.Seek(oldPosition);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct($"{nameof(GlobalValueEntry)} {Name}", this, ViewKind.GlobalValueEntry);

            s.WriteVAAnsiNullTerminatedField(nameof(Name), Name);
            s.WritePointerField(nameof(Address), Address);
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
