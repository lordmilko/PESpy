using System;
using System.Diagnostics;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_CONFIG32 / IMAGE_ENCLAVE_CONFIG64
    public struct ImageEnclaveConfig : IValue, IViewable
    {
        private const int ImportListOffset = 16;

        public int Size => chunk.PeekInt32(0);
        public int MinimumRequiredConfigSize => chunk.PeekInt32(4);
        public int PolicyFlags => chunk.PeekInt32(8);
        public int NumberOfImports => chunk.PeekInt32(12);

        private RVA<ImageEnclaveImport[]> importList;

        public RVA<ImageEnclaveImport[]> ImportList
        {
            get
            {
                if (importList.ListedOffset == 0)
                {
                    var value = chunk.PeekInt32(ImportListOffset);

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

        public int ImportEntrySize => chunk.PeekInt32(20);
        public NativeSpan<byte> FamilyID => chunk.PeekNativeSpan<byte>(24, IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        public NativeSpan<byte> ImageID => chunk.PeekNativeSpan<byte>(24 + IMAGE_ENCLAVE_SHORT_ID_LENGTH, IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        public int ImageVersion => chunk.PeekInt32(24 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
        public int SecurityVersion => chunk.PeekInt32(28 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
        public long EnclaveSize => (long) chunk.PeekPointer(32 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
        public int NumberOfThreads => chunk.PeekInt32(32 + chunk.PointerSize + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));
        public int EnclaveFlags => chunk.PeekInt32(36 + chunk.PointerSize + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH));

        public int Offset => chunk.AbsoluteOffset;

        internal static int StructSize(bool is32Bit) =>
            sizeof(int) + //Size
            sizeof(int) + //MinimumRequiredConfigSize
            sizeof(int) + //PolicyFlags
            sizeof(int) + //NumberOfImports
            sizeof(int) + //ImportList
            sizeof(int) + //ImportEntrySize
            IMAGE_ENCLAVE_SHORT_ID_LENGTH + //FamilyID
            IMAGE_ENCLAVE_SHORT_ID_LENGTH + //ImageID
            sizeof(int) + //ImageVersion
            sizeof(int) + //SecurityVersion
            (is32Bit ? 4 : 8) + //EnclaveSize
            sizeof(int) + //NumberOfThreads
            sizeof(int); //EnclaveFlags

        private readonly MemoryChunk chunk;

        internal ImageEnclaveConfig(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            importList = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(ImportList, fieldOffset: ImportListOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_ENCLAVE_CONFIG, this, ViewKind.ImageEnclaveConfig, StructSize(((PEViewWriter) writer).Is32Bit));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

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

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
