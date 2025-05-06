using System;
using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="IMAGE_DELAYLOAD_DESCRIPTOR"/> structure.
    /// </summary>
    public struct ImageDelayLoadDescriptor : IValue, IViewable
    {
#if PEFAST
        public int Attributes => chunk.PeekInt32(0);
#else
        public int Attributes { get; init; }
#endif

#if PEFAST
        private RVA<AnsiString> dllNameRVA;

        public RVA<AnsiString> DllNameRVA
        {
            get
            {
                if (dllNameRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(4);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var str = valueChunk.PeekAnsiNullTerminatedString(0);
                        dllNameRVA = new RVA<AnsiString>(rva, valueChunk.AbsoluteOffset, str);
                    }
                }

                return dllNameRVA;
            }
        }
#else
        public RVA<string> DllNameRVA { get; init; }
#endif

#if PEFAST
        private RVA<long> moduleHandleRVA;

        public RVA<long> ModuleHandleRVA
        {
            get
            {
                if (moduleHandleRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(8);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        var value = (long) chunk.PeekPointer(0);
                        moduleHandleRVA = new RVA<long>(rva, valueChunk.AbsoluteOffset, value);
                    }
                }

                return moduleHandleRVA;
            }
        }
#else
        public RVA<long> ModuleHandleRVA { get; init; }
#endif

#if PEFAST
        private RVA<ImageThunkData[]> importAddressTableRVA;

        public RVA<ImageThunkData[]> ImportAddressTableRVA
        {
            get
            {
                if (importAddressTableRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(12);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        importAddressTableRVA = ImageImportDescriptor.ParseThunks(rva, valueChunk, true);
                }

                return importAddressTableRVA;
            }
        }
#else
        public RVA<ImageThunkData[]> ImportAddressTableRVA { get; init; }
#endif

#if PEFAST
        private RVA<ImageThunkData[]> importNameTableRVA;

        public RVA<ImageThunkData[]> ImportNameTableRVA
        {
            get
            {
                if (importNameTableRVA.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(16);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                        importNameTableRVA = ImageImportDescriptor.ParseThunks(rva, valueChunk, false);
                }

                return importNameTableRVA;
            }
        }
#else
        public RVA<ImageThunkData[]> ImportNameTableRVA { get; init; }
#endif

#if PEFAST
        public int BoundImportAddressTableRVA => chunk.PeekInt32(20);
#else
        public int BoundImportAddressTableRVA { get; init; }
#endif

#if PEFAST
        private RVA<ImageThunkData[]> unloadInformationTable;

        public RVA<ImageThunkData[]> UnloadInformationTable
        {
            get
            {
                if (unloadInformationTable.ListedOffset == 0)
                {
                    var rva = chunk.PeekInt32(24);

                    if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        //When present, consists of all of the functions that were listed in the IAT

                        unloadInformationTable = ImageImportDescriptor.ParseThunks(rva, valueChunk, true);
                    }
                }

                return unloadInformationTable;
            }
        }
#else
        public RVA<ulong[]> UnloadInformationTableRVA { get; init; }
#endif

#if PEFAST
        public uint TimeDateStamp => chunk.PeekUInt32(28);
#else
        public uint TimeDateStamp { get; init; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //Attributes
            sizeof(int) + //DllNameRVA
            sizeof(int) + //ModuleHandleRVA
            sizeof(int) + //ImportAddressTableRVA
            sizeof(int) + //ImportNameTableRVA
            sizeof(int) + //BoundImportAddressTableRVA
            sizeof(int) + //UnloadInformationTableRVA
            sizeof(int);  //TimeDateStamp

#if PEFAST
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
#else
        internal ImageDelayLoadDescriptor(IFileReader reader, PEFile peFile)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Attributes = reader.ReadInt32();
            var dllNameRVA = (RVA) reader.ReadInt32();
            var moduleHandleRVA = (RVA) reader.ReadInt32();
            var importAddressTableRVA = (RVA) reader.ReadInt32();
            var importNameTableRVA = (RVA) reader.ReadInt32();
            var boundImportAddressTableRVA = (RVA) reader.ReadInt32();
            var unloadInformationTableRVA = (RVA) reader.ReadInt32();
            TimeDateStamp = reader.ReadUInt32();

            #region DllNameRVA

            if (dllNameRVA == 0)
                DllNameRVA = default;
            else
            {
                if (peFile.TryGetOffset(dllNameRVA, out var offset))
                {
                    reader.Seek(offset);
                    var str = reader.ReadAnsiNullTerminatedString();
                    DllNameRVA = new RVA<string>(dllNameRVA, offset, str);
                }
                else
                    DllNameRVA = new RVA<string>(dllNameRVA);
            }

            #endregion
            #region ModuleHandleRVA

            if (moduleHandleRVA == 0)
                ModuleHandleRVA = default;
            else
            {
                if (peFile.TryGetOffset(moduleHandleRVA, out var offset))
                {
                    //This is the position that the module handle gets stored. It may or may not be within the bounds of the physical file.
                    reader.Seek(offset);

                    if (peFile.OptionalHeader.Magic == PEMagic.PE32)
                    {
                        if (reader.TryReadInt32(out var value))
                            ModuleHandleRVA = new RVA<long>(moduleHandleRVA, offset, value);
                        else
                            ModuleHandleRVA = new RVA<long>(moduleHandleRVA);
                    }
                    else
                    {
                        if (reader.TryReadInt64(out var value))
                            ModuleHandleRVA = new RVA<long>(moduleHandleRVA, offset, value);
                        else
                            ModuleHandleRVA = new RVA<long>(moduleHandleRVA);
                    }
                }
                else
                    ModuleHandleRVA = new RVA<long>(moduleHandleRVA);
            }

            #endregion
            #region ImportAddressTableRVA

            if (importAddressTableRVA == 0)
                ImportAddressTableRVA = default;
            else
            {
                ImportAddressTableRVA = ImageImportDescriptor.ParseThunks(importAddressTableRVA, reader, peFile, null, true);
            }

            #endregion
            #region ImportNameTableRVA

            if (importNameTableRVA == 0)
                ImportNameTableRVA = default;
            else
            {
                ImportNameTableRVA = ImageImportDescriptor.ParseThunks(importNameTableRVA, reader, peFile, null, false);
            }

            #endregion
            #region BoundImportAddressTableRVA

            if (boundImportAddressTableRVA == 0)
                BoundImportAddressTableRVA = default;
            else
            {
                if (peFile.TryGetOffset(boundImportAddressTableRVA, out var offset))
                {
                    reader.Seek(offset);

                    var value = peFile.OptionalHeader.Magic == PEMagic.PE32 ? reader.ReadInt32() : reader.ReadInt64();

                    if (value != 0)
                        Debug.Assert(false, "Don't know how to handle having a BoundImportAddressTableRVA that points to something. Is it a IMAGE_BOUND_IMPORT_DESCRIPTOR? And should we be reading 4 or 8 bytes here?");

                    BoundImportAddressTableRVA = default;
                }
                else
                    BoundImportAddressTableRVA = (int) boundImportAddressTableRVA;
            }

            #endregion
            #region UnloadInformationTableRVA

            if (unloadInformationTableRVA == 0)
                UnloadInformationTableRVA = default;
            else
            {
                if (peFile.TryGetOffset(unloadInformationTableRVA, out var offset))
                {
                    reader.Seek(offset);

                    //When present, consists of all of the functions that were listed in the IAT
                    if (!ImportAddressTableRVA.IsValid)
                        throw new NotImplementedException();

                    var addresses = reader.ReadArray<ulong>(ImportAddressTableRVA.Value.Length);
                    UnloadInformationTableRVA = new RVA<ulong[]>(unloadInformationTableRVA, offset, addresses);
                }
                else
                    UnloadInformationTableRVA = new RVA<ulong[]>(unloadInformationTableRVA);
            }

            #endregion
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct($"{nameof(IMAGE_DELAYLOAD_DESCRIPTOR)} {DllNameRVA}", this, ViewKind.ImageDelayLoadDescriptor);

            //We tag delay imports so that we can group together delay import names (written as a child of the tagged delay import) separately
            //from regular import names
            using var _ = writer.EnterTag(ViewTag.DelayImport);

            s.WriteField(nameof(Attributes), Attributes);
            s.WriteRVAAnsiNullTerminatedField(nameof(DllNameRVA), DllNameRVA);
            s.WriteRVAPointerField(nameof(ModuleHandleRVA), ModuleHandleRVA);
            s.WriteField(nameof(ImportAddressTableRVA), (int) ImportAddressTableRVA.ListedOffset);
            s.WriteField(nameof(ImportNameTableRVA), (int) ImportNameTableRVA.ListedOffset);
            s.WriteField(nameof(BoundImportAddressTableRVA), BoundImportAddressTableRVA);
            s.WriteField(nameof(UnloadInformationTable), (int) UnloadInformationTable.ListedOffset);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);

            if (ImportAddressTableRVA.IsValid && ImportAddressTableRVA.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(ImportAddressTableRVA.ActualOffset, $"[DelayImportAddressTable] {DllNameRVA}", ViewKind.DelayImportAddressTable, ViewKind.ImageThunkData);

                r.WriteValues(ImportAddressTableRVA.Value);
            }

            if (ImportNameTableRVA.IsValid && ImportNameTableRVA.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(ImportNameTableRVA.ActualOffset, $"[DelayImportLookupTable] {DllNameRVA}", ViewKind.DelayImportLookupTable, ViewKind.ImageThunkData);

                r.WriteValues(ImportNameTableRVA.Value);
            }

            if (UnloadInformationTable.IsValid && UnloadInformationTable.ListedOffset != 0)
            {
                using var r = writer.CreateScopedRegion(UnloadInformationTable.ActualOffset, $"[DelayUnloadInformationTable] {DllNameRVA}", ViewKind.DelayUnloadInformationTable, ViewKind.ImageThunkData);

                r.WriteValues(UnloadInformationTable.Value);
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
