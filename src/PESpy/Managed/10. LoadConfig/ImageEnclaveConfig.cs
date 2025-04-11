using System.Diagnostics;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_CONFIG32 / IMAGE_ENCLAVE_CONFIG64
    public readonly struct ImageEnclaveConfig : IValue, IViewable
    {
        public int Size { get; }
        public int MinimumRequiredConfigSize { get; }
        public int PolicyFlags { get; }
        public int NumberOfImports { get; }
        public RVA<ImageEnclaveImport[]> ImportList { get; }
        public int ImportEntrySize { get; }
        public byte[] FamilyID { get; }
        public byte[] ImageID { get; }
        public int ImageVersion { get; }
        public int SecurityVersion { get; }
        public long EnclaveSize { get; }
        public int NumberOfThreads { get; }
        public int EnclaveFlags { get; }

        public int Offset { get; }

        internal ImageEnclaveConfig(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            Size = reader.ReadInt32();
            MinimumRequiredConfigSize = reader.ReadInt32();
            PolicyFlags = reader.ReadInt32();
            NumberOfImports = reader.ReadInt32();
            var importList = reader.ReadInt32();
            ImportEntrySize = reader.ReadInt32();
            FamilyID = reader.ReadBytes(IMAGE_ENCLAVE_SHORT_ID_LENGTH);
            ImageID = reader.ReadBytes(IMAGE_ENCLAVE_SHORT_ID_LENGTH);
            ImageVersion = reader.ReadInt32();
            SecurityVersion = reader.ReadInt32();
            EnclaveSize = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadUInt32() : reader.ReadInt64();
            NumberOfThreads = reader.ReadInt32();
            EnclaveFlags = reader.ReadInt32();

            if (peFile.TryGetOffset(importList, out var offset))
            {
                reader.Seek(offset);

                Debug.Assert(ImportEntrySize == ImageEnclaveImport.StructSize);

                var imports = new ImageEnclaveImport[NumberOfImports];

                for (var i = 0; i < NumberOfImports; i++)
                    imports[i] = new ImageEnclaveImport(reader, peFile);

                ImportList = new RVA<ImageEnclaveImport[]>(importList, offset, imports);
            }
            else
                ImportList = new RVA<ImageEnclaveImport[]>(importList);
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("IMAGE_ENCLAVE_CONFIG", this, ViewKind.ImageEnclaveConfig);

            s.WriteField(nameof(Size), Size);
            s.WriteField(nameof(MinimumRequiredConfigSize), MinimumRequiredConfigSize);
            s.WriteField(nameof(PolicyFlags), PolicyFlags);
            s.WriteField(nameof(NumberOfImports), NumberOfImports);
            s.WriteRVAField(nameof(ImportList), ImportList);
            s.WriteField(nameof(ImportEntrySize), ImportEntrySize);
            s.WriteField(nameof(FamilyID), FamilyID);
            s.WriteField(nameof(ImageID), ImageID);
            s.WriteField(nameof(ImageVersion), ImageVersion);
            s.WriteField(nameof(SecurityVersion), SecurityVersion);
            s.WriteField(nameof(EnclaveSize), EnclaveSize);
            s.WriteField(nameof(NumberOfThreads), NumberOfThreads);
            s.WriteField(nameof(EnclaveFlags), EnclaveFlags);
        }
    }
}
