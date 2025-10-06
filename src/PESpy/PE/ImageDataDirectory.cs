using System.Diagnostics;
using ClrDebug;
using PESpy.View;

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
        public int VirtualAddress => chunk.PeekInt32(0);

        /// <summary>
        /// The size of the table, in bytes.
        /// </summary>
        public int Size => chunk.PeekInt32(4);

        internal bool HasData => VirtualAddress != 0 && Size != 0;

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //RelativeVirtualAddress
            sizeof(int);  //Size

        private readonly MemoryChunk chunk;

        internal ImageDataDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

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

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
