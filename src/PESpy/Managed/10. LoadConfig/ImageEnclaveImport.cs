using PESpy.Native;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_IMPORT
    public readonly struct ImageEnclaveImport : IValue, IViewable
    {
        private const int IMAGE_ENCLAVE_SHORT_ID_LENGTH = 16;

        public IMAGE_ENCLAVE_IMPORT_MATCH MatchType { get; }

        public int MinimumSecurityVersion { get; }

        public byte[] UniqueOrAuthorID { get; }

        public byte[] FamilyID { get; }

        public byte[] ImageID { get; }

        public RVA<string> ImportName { get; }

        public int Reserved { get; }

        internal const int StructSize =
            sizeof(int) + //MatchType
            sizeof(int) + //MinimumSecurityVersion
            IMAGE_ENCLAVE_LONG_ID_LENGTH + //UniqueOrAuthorID
            IMAGE_ENCLAVE_SHORT_ID_LENGTH + //FamilyID
            IMAGE_ENCLAVE_SHORT_ID_LENGTH + //ImageID
            sizeof(int) + //ImportName
            sizeof(int); //Reserved

        public int Offset { get; }

        internal ImageEnclaveImport(ref FileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            MatchType = (IMAGE_ENCLAVE_IMPORT_MATCH) reader.ReadInt32();
            MinimumSecurityVersion = reader.ReadInt32();
            UniqueOrAuthorID = reader.ReadBytes(IMAGE_ENCLAVE_LONG_ID_LENGTH);
            FamilyID = reader.ReadBytes(IMAGE_ENCLAVE_SHORT_ID_LENGTH);
            ImageID = reader.ReadBytes(IMAGE_ENCLAVE_SHORT_ID_LENGTH);
            var importName = reader.ReadInt32();
            Reserved = reader.ReadInt32();

            //We seem to get an ImportName of 65535 when there's no name. It's not -1 because we're an int not a short
            if (importName != ushort.MaxValue && peFile.TryGetOffset(importName, out var offset))
            {
                var old = reader.Position;

                reader.Seek(offset);

                var str = reader.ReadAnsiNullTerminatedString();
                ImportName = new RVA<string>(importName, offset, str);

                reader.Seek(old);
            }
            else
                ImportName = new RVA<string>(importName);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_ENCLAVE_IMPORT), this, ViewKind.ImageEnclaveImport);

            s.WriteField(nameof(MatchType), MatchType, sizeof(int));
            s.WriteField(nameof(MinimumSecurityVersion), MinimumSecurityVersion);
            s.WriteField(nameof(UniqueOrAuthorID), UniqueOrAuthorID);
            s.WriteField(nameof(FamilyID), FamilyID);
            s.WriteField(nameof(ImageID), ImageID);
            s.WriteRVAAnsiNullTerminatedField(nameof(ImportName), ImportName);
            s.WriteField(nameof(Reserved), Reserved);
        }

        public override string ToString()
        {
            return ImportName.ToString();
        }
    }
}
