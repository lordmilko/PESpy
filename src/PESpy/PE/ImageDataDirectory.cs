using System.Diagnostics;
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RVA = System.Int32;
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DATA_DIRECTORY"/> structure that describes the location and size of a data directory that may exist in the image.<para/>
    /// Not to be confused with <see cref="ImageDebugDirectory"/>, which represents an entry within
    /// _the_ debug directory region that may be pointed to by a given <see cref="ImageDataDirectory"/>.
    /// </summary>
    [DebuggerDisplay("RVA = {VirtualAddress}, Size = {Size}")]
    public readonly struct ImageDataDirectory : IValue, IViewable //Small enough that returning a copy from properties is OK
    {
        /// <summary>
        /// The relative virtual address of the table.
        /// </summary>
#if PEFAST
        public int VirtualAddress => chunk.PeekInt32(0);
#else
        public RVA VirtualAddress { get; init; }
#endif

        /// <summary>
        /// The size of the table, in bytes.
        /// </summary>
#if PEFAST
        public int Size => chunk.PeekInt32(4);
#else
        public int Size { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //RelativeVirtualAddress
            sizeof(int);  //Size

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageDataDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal ImageDataDirectory(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            //We don't fill the FileReader buffer here; callers will typically have many data directories
            //they need to read, so we make it their responsibility to fill the buffer enough to include each
            //data directory

            VirtualAddress = (RVA) reader.ReadInt32();
            Size = reader.ReadInt32();
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DATA_DIRECTORY, this, ViewKind.ImageDataDirectory, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(VirtualAddress), (int) VirtualAddress);
            s.WriteField(nameof(Size), Size);

            return s.ToArray();
        }
    }
}
