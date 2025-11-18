using System;
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
    [DebuggerDisplay("VirtualAddress = {VirtualAddress}, Size = {Size}")]
    public readonly struct ImageDataDirectory : IViewableValue //Small enough that returning a copy from properties is OK
    {
        private const int VirtualAddressOffset = 0;
        private const int SizeOffset = 4;

        /// <summary>
        /// The relative virtual address of the table.
        /// </summary>
        public int VirtualAddress => chunk.PeekInt32(VirtualAddressOffset);

        /// <summary>
        /// The size of the table, in bytes.
        /// </summary>
        public int Size => chunk.PeekInt32(SizeOffset);

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
            //todo: need my AssertWriteGlobalsFollowRules test
            //nobody is calling writeglobals for this

            writer.WriteRVAXRef(Offset, VirtualAddressOffset, VirtualAddress);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DATA_DIRECTORY, this, ViewKind.ImageDataDirectory, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(VirtualAddress), VirtualAddressOffset, (int) VirtualAddress, FieldViewFlags.Address);
                    break;

                case 1:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size, FieldViewFlags.Size);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
