using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    public readonly struct ImageBaseRelocation : IValue, IViewable
    {
        private const int VirtualAddressOffset = 0;
        private const int SizeOfBlockOffset = 4;
        private const int EntriesOffset = 8;

        public int VirtualAddress => chunk.PeekInt32(VirtualAddressOffset);

        public int SizeOfBlock => chunk.PeekInt32(SizeOfBlockOffset);

        public NativeSpan<Entry> Entries => chunk.PeekNativeSpan<Entry>(EntriesOffset, (SizeOfBlock - 8) / 2);

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageBaseRelocation(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_BASE_RELOCATION, this, ViewKind.ImageBaseRelocation, SizeOfBlock);

        int IViewable.NumChildren() => 2 + (Entries.Length * 2);

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(VirtualAddress), VirtualAddressOffset, VirtualAddress);
                    break;

                case 1:
                    structWriter.WriteField(nameof(SizeOfBlock), SizeOfBlockOffset, SizeOfBlock);
                    break;

                default:
                    var i = (index - 2) / 2;

                    var entry = Entries[i];

                    var relativeOffset = EntriesOffset + (i * Entry.StructSize);

                    if ((index % 2) == 0)
                        structWriter.WriteBitField("Type", relativeOffset, entry.Type, sizeof(ushort), 4);
                    else
                        structWriter.WriteBitField("Offset", relativeOffset, entry.Offset, sizeof(ushort), 12);

                    break;
            }
        }

        /// <summary>
        /// Represents an entry in an <see cref="IMAGE_BASE_RELOCATION"/> record.<para/>
        /// This type encapsulates the bitfields of a <see cref="ushort"/> value and does not have a well-known native struct declaration.
        /// </summary>
        [DebuggerDisplay("Type = {Type}, Offset = {Offset}")]
        public readonly struct Entry
        {
            internal const int StructSize = sizeof(ushort);

            public IMAGE_REL_BASED Type => (IMAGE_REL_BASED) (Value >> 12);

            public short Offset => (short) (Value & 0x0FFF);

            public ushort Value { get; init; }

            //No need to pass a MemoryChunk; we read these directly through a span. The only physical
            //value is the Value field (2 bytes)
        }
    }
}
