using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.NativeAOT
{
    public struct GlobalValueEntry : IValue, IViewable
    {
        internal const int NameOffset = 0;
        private int AddressOffset => chunk.PointerSize;

        private VA<AnsiString> name;

        public VA<AnsiString> Name
        {
            get
            {
                if (name.ListedAddress == 0)
                {
                    var ptr = (long) chunk.PeekPointer(NameOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (ptr - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        name = new VA<AnsiString>(ptr, rva, str);
                    }
                    else
                        name = new VA<AnsiString>(ptr);
                }

                return name;
            }
        }

        public ulong Address => chunk.PeekPointer(AddressOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            2 * (is32Bit ? 4 : 8); //Name / Address

        private readonly MemoryChunk chunk;

        internal GlobalValueEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            name = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteVAAnsiNullTerminatedField(Name, ViewKind.GlobalValueEntry_Name, Offset, fieldOffset: NameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.GlobalValueEntry, StructSize(writer.Is32Bit));

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteVAAnsiNullTerminatedField(nameof(Name), NameOffset, Name);
                    break;

                case 1:
                    structWriter.WritePointerField(nameof(Address), AddressOffset, Address);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
