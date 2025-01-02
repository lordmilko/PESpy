using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the data contained in the reproducible debug directory.<para/>
    /// Some assemblies may have a reproducible debug directory without having any data in them.<para/>
    /// This type does not have a well-known native struct declaration.
    /// </summary>
    public readonly struct Reproducible : IValue, IViewable
    {
        public int Size { get; }

        public byte[] Hash { get; }

        public RawOffset Offset { get; }

        internal Reproducible(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Size = reader.ReadInt32();
            Hash = reader.ReadBytes(Size);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(Reproducible), this, ViewKind.Reproducible);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(Hash), Hash);
        }
    }
}
