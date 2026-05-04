using System;
using System.Diagnostics;
using PESpy.View;
using static PESpy.ImageLoadConfigDirectory;

namespace PESpy
{
    //IMAGE_ENCLAVE_CONFIG32 / IMAGE_ENCLAVE_CONFIG64
    public struct ImageEnclaveConfig : IValue, IViewable
    {
        private const int SizeOffset = 0;
        private const int MinimumRequiredConfigSizeOffset = 4;
        private const int PolicyFlagsOffset = 8;
        private const int NumberOfImportsOffset = 12;
        internal const int ImportListOffset = 16;
        private const int ImportEntrySizeOffset = 20;
        private const int FamilyIDOffset = 24;
        private const int ImageIDOffset = 24 + IMAGE_ENCLAVE_SHORT_ID_LENGTH;
        private const int ImageVersionOffset = 24 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        private const int SecurityVersionOffset = 28 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        private const int EnclaveSizeOffset = 32 + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        private int NumberOfThreadsOffset => 32 + chunk.PointerSize + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        private int EnclaveFlagsOffset => 36 + chunk.PointerSize + (2 * IMAGE_ENCLAVE_SHORT_ID_LENGTH);

        public int Size => chunk.PeekInt32(SizeOffset);
        public int MinimumRequiredConfigSize => chunk.PeekInt32(MinimumRequiredConfigSizeOffset);
        public int PolicyFlags => chunk.PeekInt32(PolicyFlagsOffset);
        public int NumberOfImports => chunk.PeekInt32(NumberOfImportsOffset);

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

        public int ImportEntrySize => chunk.PeekInt32(ImportEntrySizeOffset);
        public NativeSpan<byte> FamilyID => chunk.PeekNativeSpan<byte>(FamilyIDOffset, IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        public NativeSpan<byte> ImageID => chunk.PeekNativeSpan<byte>(ImageIDOffset, IMAGE_ENCLAVE_SHORT_ID_LENGTH);
        public int ImageVersion => chunk.PeekInt32(ImageVersionOffset);
        public int SecurityVersion => chunk.PeekInt32(SecurityVersionOffset);
        public long EnclaveSize => (long) chunk.PeekPointer(EnclaveSizeOffset);
        public int NumberOfThreads => chunk.PeekInt32(NumberOfThreadsOffset);
        public int EnclaveFlags => chunk.PeekInt32(EnclaveFlagsOffset);

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
            writer.WriteRVAField(ImportList, Offset, fieldOffset: ImportListOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ImageEnclaveConfig, StructSize(writer.Is32Bit));

        int IViewable.NumChildren() => 13;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Size), SizeOffset, Size);
                    break;

                case 1:
                    structWriter.WriteField(nameof(MinimumRequiredConfigSize), MinimumRequiredConfigSizeOffset, MinimumRequiredConfigSize);
                    break;

                case 2:
                    structWriter.WriteField(nameof(PolicyFlags), PolicyFlagsOffset, PolicyFlags);
                    break;

                case 3:
                    structWriter.WriteField(nameof(NumberOfImports), NumberOfImportsOffset, NumberOfImports);
                    break;

                case 4:
                    structWriter.WriteRVAField(nameof(ImportList), ImportListOffset, ImportList);
                    break;

                case 5:
                    structWriter.WriteField(nameof(ImportEntrySize), ImportEntrySizeOffset, ImportEntrySize);
                    break;

                case 6:
                    structWriter.WriteField(nameof(FamilyID), FamilyIDOffset, FamilyID);
                    break;

                case 7:
                    structWriter.WriteField(nameof(ImageID), ImageIDOffset, ImageID);
                    break;

                case 8:
                    structWriter.WriteField(nameof(ImageVersion), ImageVersionOffset, ImageVersion);
                    break;

                case 9:
                    structWriter.WriteField(nameof(SecurityVersion), SecurityVersionOffset, SecurityVersion);
                    break;

                case 10:
                    structWriter.WriteField(nameof(EnclaveSize), EnclaveSizeOffset, EnclaveSize);
                    break;

                case 11:
                    structWriter.WriteField(nameof(NumberOfThreads), NumberOfThreadsOffset, NumberOfThreads);
                    break;

                case 12:
                    structWriter.WriteField(nameof(EnclaveFlags), EnclaveFlagsOffset, EnclaveFlags);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
