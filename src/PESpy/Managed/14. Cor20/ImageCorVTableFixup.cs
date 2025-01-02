using ClrDebug;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_COR_VTABLEFIXUP"/> structure.
    /// </summary>
    public readonly struct ImageCorVTableFixup : IValue
    {
        public int RVA { get; }

        public short Count { get; }

        public COR_VTABLE Type { get; }

        public RawOffset Offset { get; }

        internal ImageCorVTableFixup(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            RVA = reader.ReadInt32();
            Count = reader.ReadInt16();
            Type = (COR_VTABLE) reader.ReadInt16();
        }
    }
}
