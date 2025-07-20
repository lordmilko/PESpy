using PESpy.View;

namespace PESpy
{
    public struct DebugTypeEntry : IValue, IViewable
    {
        private const int TypeNameOffset = 0;
        private int FieldNameOffset => chunk.PointerSize;

#if PEFAST
        private VA<AnsiString> typeName;

        public VA<AnsiString> TypeName
        {
            get
            {
                if (typeName.ListedAddress == 0)
                {
                    var value = (long) chunk.PeekPointer(TypeNameOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        typeName = new VA<AnsiString>(value, rva, str);
                    }
                    else
                        typeName = new VA<AnsiString>(value);
                }

                return typeName;
            }
        }

        private VA<AnsiString> fieldName;

        public VA<AnsiString> FieldName
        {
            get
            {
                if (fieldName.ListedAddress == 0)
                {
                    var value = (long) chunk.PeekPointer(FieldNameOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (value - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        fieldName = new VA<AnsiString>(value, rva, str);
                    }
                    else
                        fieldName = new VA<AnsiString>(value);
                }

                return fieldName;
            }
        }

        public int FieldOffset => chunk.PeekInt32(chunk.PointerSize * 2);

        public int ReservedPadding => chunk.PeekInt32(4 + (chunk.PointerSize * 2));

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            2 * (is32Bit ? 4 : 8) + //TypeName / FieldName
            sizeof(int) + //FieldOffset
            sizeof(int); //ReservedPadding

        private readonly MemoryChunk chunk;

        internal DebugTypeEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            typeName = default;
            fieldName = default;
        }
#else
        public VA<string> TypeName { get; }
        public VA<string> FieldName { get; }
        public int FieldOffset { get; }
        public int ReservedPadding { get; }

        public int Offset { get; }

        internal DebugTypeEntry(IFileReader reader, PEFile peFile, bool is32Bit)
        {
            Offset = (int) reader.Position;

            var typeName = is32Bit ? reader.ReadUInt32() : reader.ReadInt64();
            var fieldName = is32Bit ? reader.ReadUInt32() : reader.ReadInt64();

            FieldOffset = reader.ReadInt32();
            ReservedPadding = reader.ReadInt32();

            var oldPosition = reader.Position;

            Debug.Assert(peFile.IsLoadedImage);

            if (typeName != 0)
            {
                var actualOffset = (int) (typeName - peFile.OptionalHeader.ImageBase);
                reader.Seek(actualOffset);
                TypeName = new VA<string>(typeName, actualOffset, reader.ReadAnsiNullTerminatedString());
            }
            else
                TypeName = new VA<string>(typeName);

            if (fieldName != 0)
            {
                var actualOffset = (int) (fieldName - peFile.OptionalHeader.ImageBase);
                reader.Seek(actualOffset);
                FieldName = new VA<string>(fieldName, actualOffset, reader.ReadAnsiNullTerminatedString());
            }
            else
                FieldName = new VA<string>(fieldName);

            reader.Seek(oldPosition);
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteVAAnsiNullTerminatedField(TypeName, ViewKind.DebugTypeEntry_TypeName, fieldOffset: TypeNameOffset);
            writer.WriteVAAnsiNullTerminatedField(FieldName, ViewKind.DebugTypeEntry_FieldName, fieldOffset: FieldNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer)
        {
            var name = TypeName.ListedAddress == 0 ? $"{nameof(DebugTypeEntry)} {TypeName}" : $"{nameof(DebugTypeEntry)} {TypeName}.{FieldName}";

            return writer.NewStruct(name, this, ViewKind.DebugTypeEntry, StructSize(((PEViewWriter) writer).Is32Bit));
        }

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteVAAnsiNullTerminatedField(nameof(TypeName), TypeName);
            s.WriteVAAnsiNullTerminatedField(nameof(FieldName), FieldName);
            s.WriteField(nameof(FieldOffset), FieldOffset);
            s.WriteField(nameof(ReservedPadding), ReservedPadding);

            return s.ToArray();
        }

        public override string ToString()
        {
            return $"{TypeName}.{FieldName}";
        }
    }
}
