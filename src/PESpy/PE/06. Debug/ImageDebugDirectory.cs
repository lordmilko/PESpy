using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
using static PESpy.IMAGE_DEBUG_TYPE;

namespace PESpy
{
    /// <summary>
    /// Represents one of several <see cref="IMAGE_DEBUG_DIRECTORY"/> entries that may be pointed to by IMAGE_OPTIONAL_HEADER.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].
    /// </summary>
    public struct ImageDebugDirectory : IValue, IViewable
    {
        //Supposedly the minor version is always a certain value when it's a portable PDB, however I'm not going to rely on that
        //https://github.com/dotnet/runtime/blob/e17146d71a7e7af60e0f9320659c4677d4b68b49/src/coreclr/vm/debugdebugger.cpp#L30
        private const int PORTABLE_PDB_MINOR_VERSION = 20557; //PM

        private const int CharacteristicsOffset = 0;
        private const int TimeDateStampOffset = 4;
        private const int MajorVersionOffset = 8;
        private const int MinorVersionOffset = 10;
        private const int TypeOffset = 12;
        private const int SizeOfDataOffset = 16;
        private const int AddressOfRawDataOffset = 20;
        private const int PointerToRawDataOffset = 24;

        /// <summary>
        /// Reserved.
        /// </summary>
        public int Characteristics => chunk.PeekInt32(CharacteristicsOffset);

        /// <summary>
        /// The time and date that the debug data was created if the PE/COFF file is not deterministic,
        /// otherwise a value based on the hash of the content.
        /// </summary>
        /// <remarks>
        /// The algorithm used to calculate this value is an implementation
        /// detail of the tool that produced the file.
        /// </remarks>
        public Timestamp TimeDateStamp => chunk.PeekUInt32(TimeDateStampOffset);

        /// <summary>
        /// The major version number of the debug data format.
        /// </summary>
        public ushort MajorVersion => chunk.PeekUInt16(MajorVersionOffset);

        /// <summary>
        /// The minor version number of the debug data format.
        /// </summary>
        public ushort MinorVersion => chunk.PeekUInt16(MinorVersionOffset);

        /// <summary>
        /// The format of debugging information.
        /// </summary>
        public IMAGE_DEBUG_TYPE Type => (IMAGE_DEBUG_TYPE) chunk.PeekUInt32(TypeOffset);

        /// <summary>
        /// The size of the debug data (not including the debug directory itself).
        /// </summary>
        public int SizeOfData => chunk.PeekInt32(SizeOfDataOffset);

        /// <summary>
        /// The address of the debug data when loaded, relative to the image base.
        /// </summary>
        public int AddressOfRawData => chunk.PeekInt32(AddressOfRawDataOffset);

        /// <summary>
        /// The file pointer to the debug data.
        /// </summary>
        public int PointerToRawData => chunk.PeekInt32(PointerToRawDataOffset);

        /// <summary>
        /// Gets the data pointed to by this directory.<para/>
        /// This value is not part of the native type definition.
        /// </summary>
        private object? data;

        public object? Data
        {
            get
            {
                if (data == null)
                {
                    if (TryGetValueChunk(out var valueChunk))
                    {
                        switch (Type)
                        {
                            case IMAGE_DEBUG_TYPE_UNKNOWN:
                                if (SizeOfData == 0)
                                    return null;

                                goto default;

                            case IMAGE_DEBUG_TYPE_COFF:
                            {
                                data = new ImageCoffSymbolsHeader(valueChunk);
                                break;
                            }

                            case IMAGE_DEBUG_TYPE_CODEVIEW:
                                data = ReadCodeView(valueChunk, SizeOfData);
                                break;

                            case IMAGE_DEBUG_TYPE_FPO:
                            {
                                var numEntries = SizeOfData / FpoData.StructSize;

                                var entries = new FpoData[numEntries];

                                for (var i = 0; i < numEntries; i++)
                                    entries[i] = new FpoData(valueChunk.Slice(i * FpoData.StructSize));

                                data = entries;
                                break;
                            }

                            case IMAGE_DEBUG_TYPE_MISC:
                                data = new ImageDebugMisc(valueChunk);
                                break;

                            case IMAGE_DEBUG_TYPE_EXCEPTION:
                                goto default;

                            case IMAGE_DEBUG_TYPE_FIXUP:
                            {
                                var entries = new XFixupData[SizeOfData / XFixupData.StructSize];

                                for (var i = 0; i < entries.Length; i++)
                                    entries[i] = new XFixupData(valueChunk.Slice(i * XFixupData.StructSize));

                                data = entries;
                                break;
                            }

                            case IMAGE_DEBUG_TYPE_OMAP_TO_SRC: //OMAP type?
                            case IMAGE_DEBUG_TYPE_OMAP_FROM_SRC: //OMAP type?
                            case IMAGE_DEBUG_TYPE_BORLAND:
                                goto default;

                            case IMAGE_DEBUG_TYPE_RESERVED10:
                                //C:\Windows\system32\FM20.dll has this with a size of 4. Nobody knows what to do with this directory however
                                //Format seems to be BB 00 and then two more bytes. aspnet_filter.dll had BB 03
                                if (SizeOfData > 0) //Don't know that it can be 0, but good to be defensive
                                {
                                    Debug.Assert(SizeOfData == 4);
                                    data = new ByteBlob(valueChunk, SizeOfData);
                                }
                                else
                                    data = default;
                                break;

                            case IMAGE_DEBUG_TYPE_CLSID:
                                goto default;

                            case IMAGE_DEBUG_TYPE_VC_FEATURE:
                                data = new VCFeature(valueChunk);
                                break;

                            case IMAGE_DEBUG_TYPE_POGO:
                                //Are they maybe called IMAGE_POGO_BLOCK and IMAGE_POGO_INFO? Need more citations
                                data = ReadPogo(valueChunk, SizeOfData);
                                break;

                            case IMAGE_DEBUG_TYPE_ILTCG:
                                //I've seen this with size 0
                                if (SizeOfData != 0)
                                    goto default;
                                else
                                    data = default;
                                break;

                            case IMAGE_DEBUG_TYPE_MPX:
                                goto default;

                            case IMAGE_DEBUG_TYPE_REPRO:
                                //dotnet/runtime says that this directory must be empty, but that's not true, it can sometimes have a hash.
                                //it can also sometimes be empty as well
                                if (SizeOfData != 0)
                                    data = new Reproducible(valueChunk);
                                else
                                    data = default;
                                break;

                            case IMAGE_DEBUG_TYPE_EMBEDDED_PORTABLE_PDB:
                                data = new EmbeddedPortablePdb(valueChunk, SizeOfData);
                                break;

                            case IMAGE_DEBUG_TYPE_SPGO:
                                goto default;

                            case IMAGE_DEBUG_TYPE_PDB_CHECKSUM:
                                data = new PdbChecksum(valueChunk, SizeOfData);
                                break;

                            case IMAGE_DEBUG_TYPE_EX_DLLCHARACTERISTICS:
                                if (SizeOfData == 4)
                                {
                                    var value = (IMAGE_DLLCHARACTERISTICS_EX) valueChunk.PeekInt32(0);
                                    data = new RawValue<IMAGE_DLLCHARACTERISTICS_EX>(valueChunk.AbsoluteOffset, value);
                                }
                                else if (SizeOfData > 0) //Defensively check for 0 length. Has never known to not be 4 bytes
                                    data = new ByteBlob(valueChunk, SizeOfData);
                                else
                                    data = default;
                                break;

                            case IMAGE_DEBUG_TYPE_R2R_PERFMAP:
                            default:
#if DEBUG
                                Debug.Assert(false, $"Reading a debug directory of type '{Type}' with size {SizeOfData} is not implemented");
#endif
                                //Defensively check for 0 length
                                if (SizeOfData > 0)
                                    data = new ByteBlob(valueChunk, SizeOfData);
                                else
                                    data = default;
                                break;
                        }
                    }
                }

                return data;
            }
        }

        public bool IsPortablePDB => MinorVersion == PORTABLE_PDB_MINOR_VERSION;

        private bool TryGetValueChunk(out MemoryChunk valueChunk)
        {
            if (chunk.block is GlobalMemoryBlock b)
            {
                //It's a DBG file
                if (PointerToRawData != 0)
                {
                    valueChunk = new MemoryChunk(b, PointerToRawData);
                    return true;
                }

                valueChunk = default;
                return false;
            }

            //ImageDebugDirectory stores both AddressOfRawData and PointerToRawData. This ultimately doesn't help us however, as we still need to know
            //which MemoryBlock should own the corresponding memory
            var peFile = chunk.PEFile();

            //We can't just use AddressOfRawData, because in an unloaded image that might be 0
            if (peFile.IsLoadedImage)
            {
                return peFile.TryGetValueChunkFromSection(AddressOfRawData, out valueChunk);
            }
            else
            {
                return peFile.TryGetValueChunkFromPhysicalOffset(PointerToRawData, out valueChunk);
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(uint) +   // Characteristics
            sizeof(uint) +   // TimeDataStamp
            sizeof(uint) +   // Version
            sizeof(uint) +   // Type
            sizeof(uint) +   // SizeOfData
            sizeof(uint) +   // AddressOfRawData
            sizeof(uint);    // PointerToRawData

        private readonly MemoryChunk chunk;

        internal ImageDebugDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            data = default;

#if STRESS_TEST
            _ = Data;
#endif
        }

        #region Data

        private static IValue? ReadCodeView(in MemoryChunk chunk, int sizeOfData)
        {
            var sig = (CodeViewSig) chunk.PeekUInt32(0);

            switch (sig)
            {
                //PDB v2.0
                case CodeViewSig.NB10:
                    return new NB10I(chunk);

                //OMF
                case CodeViewSig.NB09: //Note that I don't think you can actually have NB09 in a PE file
                case CodeViewSig.NB11:
                    return OMFReader.ReadNB05(chunk, sig, chunk.PeekInt32(sizeOfData - 4), sizeOfData); //The last 4 bytes of the data should be lfoBase, which should be the same value as sizeOfData as well

                //PDB v7.0
                case CodeViewSig.RSDS:
                    return new RSDSI(chunk);

                default:
                    Debug.Assert(false, $"Don't know how to read CodeView signature '{chunk.PeekAnsiFixedLength(0, 4)}' (0x{chunk.PeekUInt32(0):X})");

                    //Unsupported value; read as a byte blob
                    return new ByteBlob(chunk, sizeOfData);
            }
        }

        private static IValue ReadPogo(in MemoryChunk chunk, int sizeOfData)
        {
            var signature = (PogoSignatureKind) chunk.PeekUInt32(0);

            switch (signature)
            {
                case PogoSignatureKind.Zero:
                case PogoSignatureKind.LCTG:
                case PogoSignatureKind.PGI:
                case PogoSignatureKind.PGO:
                case PogoSignatureKind.PGU:
                case PogoSignatureKind.SPGO:
                    return new PogoData(chunk, sizeOfData);

                default:
                    Debug.Assert(false, $"Don't know how to read Pogo signature '{signature:X}'");

                    //Unsupported value; read as a byte blob
                    return new ByteBlob(chunk, sizeOfData);
            }
        }

        #endregion

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            if (Data != null)
            {
                if (Data is IViewable v)
                    writer.WriteGlobal(v);
                else if (Data is RawValue<IMAGE_DLLCHARACTERISTICS_EX> r)
                    writer.WriteGlobal(r.Offset, r.Value, sizeof(int), ViewKind.ExDllCharacteristics);
                else if (Data is FpoData[] f)
                    writer.WriteGlobal(f);
                else if (Data is XFixupData[] x)
                    writer.WriteGlobal(x);
                else
                    throw new NotImplementedException($"Don't know how to write a value of type {Data.GetType().Name}");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DEBUG_DIRECTORY, this, ViewKind.ImageDebugDirectory, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(Characteristics), CharacteristicsOffset, Characteristics);
                    break;

                case 1:
                    structWriter.WriteField(nameof(TimeDateStamp), TimeDateStampOffset, TimeDateStamp);
                    break;

                case 2:
                    structWriter.WriteField(nameof(MajorVersion), MajorVersionOffset, MajorVersion);
                    break;

                case 3:
                    structWriter.WriteField(nameof(MinorVersion), MinorVersionOffset, MinorVersion);
                    break;

                case 4:
                    structWriter.WriteField(nameof(Type), TypeOffset, Type, sizeof(int));
                    break;

                case 5:
                    structWriter.WriteField(nameof(SizeOfData), SizeOfDataOffset, SizeOfData, FieldViewFlags.Size);
                    break;

                case 6:
                    structWriter.WriteField(nameof(AddressOfRawData), AddressOfRawDataOffset, AddressOfRawData, FieldViewFlags.Address);
                    break;

                case 7:
                    structWriter.WriteField(nameof(PointerToRawData), PointerToRawDataOffset, PointerToRawData, FieldViewFlags.Address);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal static bool TryGetSymbolAccessor(IFile file, ImageDebugDirectory[]? debugTable, out ISymbolAccessor symbolAccessor)
        {
            if (debugTable == null)
            {
                symbolAccessor = default;
                return false;
            }

            //Prefer CodeView, and fallback to COFF

            NB05Data codeView = null;
            CoffSymbolTable coff = null;

            for (var i = 0; i < debugTable.Length; i++)
            {
                ref var debugDirectory = ref debugTable[i];

                switch (debugDirectory.Type)
                {
                    case IMAGE_DEBUG_TYPE_CODEVIEW:
                        codeView = debugDirectory.Data as NB05Data;
                        break;

                    case IMAGE_DEBUG_TYPE_COFF:
                        coff = ((ImageCoffSymbolsHeader) debugDirectory.Data!).LvaToFirstSymbol.ValueOrDefault;
                        break;
                }
            }

            if (codeView != null)
            {
                symbolAccessor = (ISymbolAccessor) codeView.GetCodeViewAccessor();
                return true;
            }

            if (coff != null)
            {
                var sectionHeaders = file.Kind switch
                {
                    FileKind.PE => ((PEFile) file).SectionHeaders,
                    FileKind.DBG => ((DBGFile) file).SectionHeaders,
                };

                symbolAccessor = new CoffSymbolAccessor(coff, sectionHeaders);
                return true;
            }

            symbolAccessor = default;
            return false;
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
