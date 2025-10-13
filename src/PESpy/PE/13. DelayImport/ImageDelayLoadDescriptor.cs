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
        internal const int DllNameRVAOffset = 4;
        internal const int ModuleHandleRVAOffset = 8;
        internal const int ImportAddressTableRVAOffset = 12;
        internal const int ImportNameTableRVAOffset = 16;
        internal const int UnloadInformationTableOffset = 24;

        public int Attributes => chunk.PeekInt32(0);

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

        public int BoundImportAddressTableRVA => chunk.PeekInt32(20);

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

        public Timestamp TimeDateStamp => chunk.PeekUInt32(28);

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
            //We tag delay imports so that we can group together delay import names (written as a child of the tagged delay import) separately
            //from regular import names
            using var _ = writer.EnterTag(ViewTag.DelayImport);

            writer.WriteRVAAnsiNullTerminatedField(DllNameRVA, ViewKind.ImageDelayLoadDescriptor_DllNameRVA, fieldOffset: DllNameRVAOffset);
            writer.WriteRVAPointerField(ModuleHandleRVA, ModuleHandleRVAOffset);

            if (ImportAddressTableRVA.IsValid && ImportAddressTableRVA.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(
                    ImportAddressTableRVA.ActualOffset,
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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Attributes), Attributes);
            s.WriteRVAAnsiNullTerminatedField(nameof(DllNameRVA), DllNameRVA);
            s.WriteRVAPointerField(nameof(ModuleHandleRVA), ModuleHandleRVA);
            s.WriteRVAField(nameof(ImportAddressTableRVA), ImportAddressTableRVA);
            s.WriteRVAField(nameof(ImportNameTableRVA), ImportNameTableRVA);
            s.WriteField(nameof(BoundImportAddressTableRVA), BoundImportAddressTableRVA); //Should be WriteRVAField but we don't yet know what it points to
            s.WriteRVAField(nameof(UnloadInformationTable), UnloadInformationTable);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            if (DllNameRVA.ListedOffset == 0)
                return "0";

            return DllNameRVA.ToString();
        }
    }
}
