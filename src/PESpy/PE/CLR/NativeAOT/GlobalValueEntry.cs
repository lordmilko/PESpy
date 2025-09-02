using PESpy.View;

namespace PESpy
{
    public struct GlobalValueEntry : IValue, IViewable
    {
        private const int NameOffset = 0;

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

        public ulong Address => chunk.PeekPointer(chunk.PointerSize);

        public int Offset => chunk.AbsoluteOffset;

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
            writer.WriteVAAnsiNullTerminatedField(Name, ViewKind.GlobalValueEntry_Name, fieldOffset: NameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.GlobalValueEntry, this, ViewKind.GlobalValueEntry, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteVAAnsiNullTerminatedField(nameof(Name), Name);
            s.WritePointerField(nameof(Address), Address);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
