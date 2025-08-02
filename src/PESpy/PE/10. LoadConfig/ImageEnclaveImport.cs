using System;
using PESpy.Native;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_IMPORT
    public struct ImageEnclaveImport : IValue, IViewable
    {
        private const int IMAGE_ENCLAVE_SHORT_ID_LENGTH = 16;
        private const int ImportNameOffset = 8 + IMAGE_ENCLAVE_LONG_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH;

        public IMAGE_ENCLAVE_IMPORT_MATCH MatchType => (IMAGE_ENCLAVE_IMPORT_MATCH) chunk.PeekUInt32(0);

        public int MinimumSecurityVersion => chunk.PeekInt32(4);

        public NativeSpan<byte> UniqueOrAuthorID => chunk.PeekNativeSpan<byte>(8, IMAGE_ENCLAVE_LONG_ID_LENGTH);

        public NativeSpan<byte> FamilyID => chunk.PeekNativeSpan<byte>(8 + IMAGE_ENCLAVE_LONG_ID_LENGTH, IMAGE_ENCLAVE_SHORT_ID_LENGTH);

        public NativeSpan<byte> ImageID => chunk.PeekNativeSpan<byte>(8 + IMAGE_ENCLAVE_LONG_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH, IMAGE_ENCLAVE_SHORT_ID_LENGTH);

        private RVA<AnsiString>? importName;

        public RVA<AnsiString> ImportName
        {
            get
            {
                if (importName == null)
                {
                    var rva = chunk.PeekInt32(ImportNameOffset);

                    //We seem to get an ImportName of 65535 when there's no name. It's not -1 because we're an int not a short
                    if (rva != ushort.MaxValue && chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        return new RVA<AnsiString>(rva, valueChunk.AbsoluteOffset, str);
                    }
                    else
                        importName = new RVA<AnsiString>(rva);
                }

                return importName.Value;
            }
        }

        public int Reserved => chunk.PeekInt32(12 + IMAGE_ENCLAVE_LONG_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH);

        internal const int StructSize =
            sizeof(int) + //MatchType
            sizeof(int) + //MinimumSecurityVersion
            IMAGE_ENCLAVE_LONG_ID_LENGTH + //UniqueOrAuthorID
            IMAGE_ENCLAVE_SHORT_ID_LENGTH + //FamilyID
            IMAGE_ENCLAVE_SHORT_ID_LENGTH + //ImageID
            sizeof(int) + //ImportName
            sizeof(int); //Reserved

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal ImageEnclaveImport(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            importName = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAAnsiNullTerminatedField(ImportName, ViewKind.ImageEnclaveImport_ImportName, fieldOffset: ImportNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_ENCLAVE_IMPORT, this, ViewKind.ImageEnclaveImport, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(MatchType), MatchType, sizeof(int));
            s.WriteField(nameof(MinimumSecurityVersion), MinimumSecurityVersion);
            s.WriteField(nameof(UniqueOrAuthorID), UniqueOrAuthorID);
            s.WriteField(nameof(FamilyID), FamilyID);
            s.WriteField(nameof(ImageID), ImageID);
            s.WriteRVAAnsiNullTerminatedField(nameof(ImportName), ImportName);
            s.WriteField(nameof(Reserved), Reserved);

            return s.ToArray();
        }

        public override string ToString()
        {
            return ImportName.ToString();
        }
    }
}
