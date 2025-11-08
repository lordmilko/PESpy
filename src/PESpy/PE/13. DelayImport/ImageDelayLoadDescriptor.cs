using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DELAYLOAD_DESCRIPTOR"/> structure.
    /// </summary>
    public struct ImageDelayLoadDescriptor : IValue, IViewable
    {
        private const int AttributesOffset = 0;
        internal const int DllNameRVAOffset = 4;
        internal const int ModuleHandleRVAOffset = 8;
        internal const int ImportAddressTableRVAOffset = 12;
        internal const int ImportNameTableRVAOffset = 16;
        private const int BoundImportAddressTableRVAOffset = 20;
        private const int UnloadInformationTableOffset = 24;
        private const int TimeDateStampOffset = 28;

        public int Attributes => chunk.PeekInt32(AttributesOffset);

        private RVA<AnsiString> dllNameRVA;

        public RVA<AnsiString> DllNameRVA
        {
            get
            {
                if (dllNameRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(DllNameRVAOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        dllNameRVA = new RVA<AnsiString>(rva, valueChunk.AbsoluteOffset, str);
                    }
                }

                return dllNameRVA;
            }
        }

        private RVA<long> moduleHandleRVA;

        public RVA<long> ModuleHandleRVA
        {
            get
            {
                if (moduleHandleRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(ModuleHandleRVAOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var value = (long) valueChunk.PeekPointer(0);
                        moduleHandleRVA = new RVA<long>(rva, valueChunk.AbsoluteOffset, value);
                    }
                }

                return moduleHandleRVA;
            }
        }

        private RVA<ImageThunkData[]> importAddressTableRVA;

        public RVA<ImageThunkData[]> ImportAddressTableRVA
        {
            get
            {
                if (importAddressTableRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(ImportAddressTableRVAOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        importAddressTableRVA = ImageImportDescriptor.ParseThunks(rva, valueChunk, true);
                }

                return importAddressTableRVA;
            }
        }

        private RVA<ImageThunkData[]> importNameTableRVA;

        public RVA<ImageThunkData[]> ImportNameTableRVA
        {
            get
            {
                if (importNameTableRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(ImportNameTableRVAOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        importNameTableRVA = ImageImportDescriptor.ParseThunks(rva, valueChunk, false);
                }

                return importNameTableRVA;
            }
        }

        public int BoundImportAddressTableRVA => chunk.PeekInt32(BoundImportAddressTableRVAOffset);

        private RVA<ImageThunkData[]> unloadInformationTable;

        public RVA<ImageThunkData[]> UnloadInformationTable
        {
            get
            {
                if (unloadInformationTable.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(UnloadInformationTableOffset);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        //When present, consists of all of the functions that were listed in the IAT

                        unloadInformationTable = ImageImportDescriptor.ParseThunks(rva, valueChunk, true);
                    }
                }

                return unloadInformationTable;
            }
        }

        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //Attributes
            sizeof(int) + //DllNameRVA
            sizeof(int) + //ModuleHandleRVA
            sizeof(int) + //ImportAddressTableRVA
            sizeof(int) + //ImportNameTableRVA
            sizeof(int) + //BoundImportAddressTableRVA
            sizeof(int) + //UnloadInformationTableRVA
            sizeof(int);  //TimeDateStamp

        private readonly MemoryChunk chunk;

        internal ImageDelayLoadDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            dllNameRVA = default;
            importAddressTableRVA = default;
            importNameTableRVA = default;
            moduleHandleRVA = default;
            unloadInformationTable = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var structOffset = Offset;

            //We tag delay imports so that we can group together delay import names (written as a child of the tagged delay import) separately
            //from regular import names
            using var _ = writer.EnterTag(ViewTag.DelayImport);

            writer.WriteRVAAnsiNullTerminatedField(DllNameRVA, ViewKind.ImageDelayLoadDescriptor_DllNameRVA, structOffset, fieldOffset: DllNameRVAOffset);
            writer.WriteRVAPointerField(ModuleHandleRVA, structOffset, fieldOffset: ModuleHandleRVAOffset);

            if (ImportAddressTableRVA.IsValid && ImportAddressTableRVA.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    ImportAddressTableRVA.ActualOffset,
                    structOffset,
                    ImportAddressTableRVAOffset,
                    $"[DelayImportAddressTable] {DllNameRVA}",
                    ViewKind.DelayImportAddressTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , ImportAddressTableRVA.ListedOffset
#endif
                );

                r.WriteValues(ImportAddressTableRVA.Value);
            }

            if (ImportNameTableRVA.IsValid && ImportNameTableRVA.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    ImportNameTableRVA.ActualOffset,
                    structOffset,
                    ImportNameTableRVAOffset,
                    $"[DelayImportLookupTable] {DllNameRVA}",
                    ViewKind.DelayImportLookupTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , ImportNameTableRVA.ListedOffset
#endif
                );

                r.WriteValues(ImportNameTableRVA.Value);
            }

            if (UnloadInformationTable.IsValid && UnloadInformationTable.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    UnloadInformationTable.ActualOffset,
                    structOffset,
                    UnloadInformationTableOffset,
                    $"[DelayUnloadInformationTable] {DllNameRVA}",
                    ViewKind.DelayUnloadInformationTable,
                    ViewKind.ImageThunkData
#if DEBUG
                    , UnloadInformationTable.ListedOffset
#endif
                );

                r.WriteValues(UnloadInformationTable.Value);
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DELAYLOAD_DESCRIPTOR, this, ViewKind.ImageDelayLoadDescriptor, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Attributes), AttributesOffset, Attributes);
                    break;

                case 1:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(DllNameRVA), DllNameRVAOffset, DllNameRVA);
                    break;

                case 2:
                    structWriter.WriteRVAPointerField(nameof(ModuleHandleRVA), ModuleHandleRVAOffset, ModuleHandleRVA);
                    break;

                case 3:
                    structWriter.WriteRVAField(nameof(ImportAddressTableRVA), ImportAddressTableRVAOffset, ImportAddressTableRVA);
                    break;

                case 4:
                    structWriter.WriteRVAField(nameof(ImportNameTableRVA), ImportNameTableRVAOffset, ImportNameTableRVA);
                    break;

                case 5:
                    structWriter.WriteField(nameof(BoundImportAddressTableRVA), BoundImportAddressTableRVAOffset, BoundImportAddressTableRVA); //Should be WriteRVAField but we don't yet know what it points to
                    break;

                case 6:
                    structWriter.WriteRVAField(nameof(UnloadInformationTable), UnloadInformationTableOffset, UnloadInformationTable);
                    break;

                case 7:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            if (DllNameRVA.ListedOffset == 0)
                return "0";

            return DllNameRVA.ToString();
        }
    }
}
