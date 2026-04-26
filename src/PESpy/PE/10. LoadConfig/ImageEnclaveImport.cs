using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_IMPORT
    public struct ImageEnclaveImport : IValue, IViewable
    {
        private const int IMAGE_ENCLAVE_SHORT_ID_LENGTH = 16;

        private const int MatchTypeOffset = 0;
        private const int MinimumSecurityVersionOffset = 4;
        private const int UniqueOrAuthorIDOffset = 8;
        private const int FamilyIDOffset = 8 + IMAGE_ENCLAVE_LONG_ID_LENGTH;
        private const int ImageIDOffset = 8 + IMAGE_ENCLAVE_LONG_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH;
        internal const int ImportNameOffset = 8 + IMAGE_ENCLAVE_LONG_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH;
        private const int ReservedOffset = 12 + IMAGE_ENCLAVE_LONG_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH + IMAGE_ENCLAVE_SHORT_ID_LENGTH;

        public IMAGE_ENCLAVE_IMPORT_MATCH MatchType => (IMAGE_ENCLAVE_IMPORT_MATCH) chunk.PeekUInt32(MatchTypeOffset);

        public int MinimumSecurityVersion => chunk.PeekInt32(MinimumSecurityVersionOffset);

        public NativeSpan<byte> UniqueOrAuthorID => chunk.PeekNativeSpan<byte>(UniqueOrAuthorIDOffset, IMAGE_ENCLAVE_LONG_ID_LENGTH);

        public NativeSpan<byte> FamilyID => chunk.PeekNativeSpan<byte>(FamilyIDOffset, IMAGE_ENCLAVE_SHORT_ID_LENGTH);

        public NativeSpan<byte> ImageID => chunk.PeekNativeSpan<byte>(ImageIDOffset, IMAGE_ENCLAVE_SHORT_ID_LENGTH);

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

        public int Reserved => chunk.PeekInt32(ReservedOffset);

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
            //This name may also be written by ImageImportDescriptor
            writer.WriteUniqueRVAAnsiNullTerminatedField(ImportName, ViewKind.ImageEnclaveImport_ImportName, Offset, fieldOffset: ImportNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageEnclaveImport, StructSize);

        int IViewable.NumChildren() => 7;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(MatchType), MatchTypeOffset, MatchType, sizeof(int));
                    break;

                case 1:
                    structWriter.WriteField(nameof(MinimumSecurityVersion), MinimumSecurityVersionOffset, MinimumSecurityVersion);
                    break;

                case 2:
                    structWriter.WriteField(nameof(UniqueOrAuthorID), UniqueOrAuthorIDOffset, UniqueOrAuthorID);
                    break;

                case 3:
                    structWriter.WriteField(nameof(FamilyID), FamilyIDOffset, FamilyID);
                    break;

                case 4:
                    structWriter.WriteField(nameof(ImageID), ImageIDOffset, ImageID);
                    break;

                case 5:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(ImportName), ImportNameOffset, ImportName);
                    break;

                case 6:
                    structWriter.WriteField(nameof(Reserved), ReservedOffset, Reserved);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return ImportName.ToString();
        }
    }
}
