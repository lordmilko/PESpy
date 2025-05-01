using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct DebugTypeEntry : IValue, IViewable
    {
        public int Offset { get; }

        public VA<string> TypeName { get; }
        public VA<string> FieldName { get; }
        public int FieldOffset { get; }
        public int ReservedPadding { get; }

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

        void IViewable.WriteView(ViewWriter writer)
        {
            var name = TypeName.ListedAddress == 0 ? $"{nameof(DebugTypeEntry)} {TypeName}" : $"{nameof(DebugTypeEntry)} {TypeName}.{FieldName}";

            using var s = writer.CreateStruct(name, this, ViewKind.DebugTypeEntry);

            s.WriteVAAnsiNullTerminatedField(nameof(TypeName), TypeName);
            s.WriteVAAnsiNullTerminatedField(nameof(FieldName), FieldName);
            s.WriteField(nameof(FieldOffset), FieldOffset);
            s.WriteField(nameof(ReservedPadding), ReservedPadding);
        }

        public override string ToString()
        {
            return $"{TypeName}.{FieldName}";
        }
    }
}
