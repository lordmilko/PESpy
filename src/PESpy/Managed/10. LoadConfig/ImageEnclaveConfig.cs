using System;
using System.Diagnostics;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_CONFIG32 / IMAGE_ENCLAVE_CONFIG64
    public struct ImageEnclaveConfig : IValue, IViewable
    {
#if PEFAST
        public int Size => chunk.PeekInt32(0);
#else
        public int Size { get; }
#endif
#if PEFAST
        public int MinimumRequiredConfigSize => chunk.PeekInt32(4);
#else
        public int MinimumRequiredConfigSize { get; }
#endif
#if PEFAST
        public int PolicyFlags => chunk.PeekInt32(8);
#else
        public int PolicyFlags { get; }
#endif
#if PEFAST
        public int NumberOfImports => chunk.PeekInt32(12);
#else
        public int NumberOfImports { get; }
#endif

#if PEFAST
        private RVA<ImageEnclaveImport[]> importList;

        public RVA<ImageEnclaveImport[]> ImportList
        {
            get
            {
                if (importList.ListedOffset == 0)
                {
                    var value = chunk.PeekInt32(16);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(value, out var valueChunk))
                    {
                        Debug.Assert(ImportEntrySize == ImageEnclaveImport.StructSize);

                        var imports = new ImageEnclaveImport[NumberOfImports];

                        for (var i = 0; i < NumberOfImports; i++)
                            imports[i] = new ImageEnclaveImport(valueChunk.Slice(i * ImageEnclaveImport.StructSize));

                        importList = new RVA<ImageEnclaveImport[]>(value, valueChunk.AbsoluteOffset, imports);
                    }
                    else
                        importList = new RVA<ImageEnclaveImport[]>(value);
                }

                return importList;
            }
        }
#else
        public RVA<ImageEnclaveImport[]> ImportList { get; }
#endif

#if PEFAST
        public int ImportEntrySize => chunk.PeekInt32(20);
#else
        public int ImportEntrySize { get; }
#endif
#if PEFAST
        public Span<byte> FamilyID => chunk.PeekSpan<byte>(24, IMAGE_ENCLAVE_SHORT_ID_LENGTH);
#else
        public byte[] FamilyID { get; }
#endif
#if PEFAST
        public Span<byte> ImageID => chunk.PeekSpan<byte>(24 + IMAGE_ENCLAVE_SHORT_ID_LENGTH, IMAGE_ENCLAVE_SHORT_ID_LENGTH);
#else
        public byte[] ImageID { get; }
#endif
#if PEFAST
        public int ImageVersion => chunk.PeekInt32(24 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
#else
        public int ImageVersion { get; }
#endif
#if PEFAST
        public int SecurityVersion => chunk.PeekInt32(28 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
#else
        public int SecurityVersion { get; }
#endif
#if PEFAST
        public long EnclaveSize => (long) chunk.PeekPointer(32 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
#else
        public long EnclaveSize { get; }
#endif
#if PEFAST
        public int NumberOfThreads => chunk.PeekInt32(32 + chunk.PointerSize + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
#else
        public int NumberOfThreads { get; }
#endif
#if PEFAST
        public int EnclaveFlags => chunk.PeekInt32(36 + chunk.PointerSize + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
#else
        public int EnclaveFlags { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageEnclaveConfig(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            importList = default;
        }
#else
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
#endif

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
