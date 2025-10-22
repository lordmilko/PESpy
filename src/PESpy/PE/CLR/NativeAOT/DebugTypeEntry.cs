using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct DebugTypeEntry : IValue, IViewable
    {
        private const int TypeNameOffset = 0;
        private int FieldNameOffset => chunk.PointerSize;
        private int FieldOffsetOffset => chunk.PointerSize * 2;
        private int ReservedPaddingOffset => 4 + (chunk.PointerSize * 2);

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

        public int FieldOffset => chunk.PeekInt32(FieldOffsetOffset);

        public int ReservedPadding => chunk.PeekInt32(ReservedPaddingOffset);

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteVAAnsiNullTerminatedField(TypeName, ViewKind.DebugTypeEntry_TypeName, fieldOffset: TypeNameOffset);
            writer.WriteVAAnsiNullTerminatedField(FieldName, ViewKind.DebugTypeEntry_FieldName, fieldOffset: FieldNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.DebugTypeEntry, this, ViewKind.DebugTypeEntry, StructSize(chunk.Is32Bit));

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteVAAnsiNullTerminatedField(nameof(TypeName), TypeNameOffset, TypeName);
                    break;

                case 1:
                    structWriter.WriteVAAnsiNullTerminatedField(nameof(FieldName), FieldNameOffset, FieldName);
                    break;

                case 2:
                    structWriter.WriteField(nameof(FieldOffset), FieldOffsetOffset, FieldOffset);
                    break;

                case 3:
                    structWriter.WriteField(nameof(ReservedPadding), ReservedPaddingOffset, ReservedPadding);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return $"{TypeName}.{FieldName}";
        }
    }
}
