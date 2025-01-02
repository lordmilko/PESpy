using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents a Profile Guided Optimization entry. This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct PogoItem : IValue, IViewable
    {
        public int RVA { get; }

        public int Size { get; }

        public string Name { get; }

        public RawOffset Offset { get; }

        internal PogoItem(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            RVA = reader.ReadInt32();
            Size = reader.ReadInt32();
            Name = reader.ReadAnsiNullTerminatedString();

            //Each entry should be aligned to 4 bytes
            var alignmentTarget = (reader.Position + 3) & ~3;

            while (reader.Position < alignmentTarget)
                reader.ReadByte();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PogoItem), this, ViewKind.PogoItem);

            s.WriteField(nameof(RVA), RVA);
            s.WriteField(nameof(Size), Size);
            s.WriteAnsiNullTerminatedField(nameof(Name), Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
