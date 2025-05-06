using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct GlobalValueEntry : IValue, IViewable
    {
#if PEFAST
        private VA<AnsiString> name;

        public VA<AnsiString> Name
        {
            get
            {
                if (name.ListedAddress == 0)
                {
                    var ptr = (long) chunk.PeekPointer(0);

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

        private readonly MemoryChunk chunk;

        internal GlobalValueEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            name = default;
        }
#else
        public VA<string> Name { get; }
        public long Address { get; }

        public int Offset { get; }

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
#endif

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
