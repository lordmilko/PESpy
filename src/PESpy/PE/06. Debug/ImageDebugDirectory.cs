using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents one of several <see cref="IMAGE_DEBUG_DIRECTORY"/> entries that may be pointed to by IMAGE_OPTIONAL_HEADER.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].
    /// </summary>
    public struct ImageDebugDirectory : IValue, IViewable
    {
        private const int PORTABLE_PDB_MINOR_VERSION = 20557; //PM

        /// <summary>
        /// Reserved.
        /// </summary>
        public int Characteristics => chunk.PeekInt32(0);

        /// <summary>
        /// The time and date that the debug data was created if the PE/COFF file is not deterministic,
        /// otherwise a value based on the hash of the content.
        /// </summary>
        /// <remarks>
        /// The algorithm used to calculate this value is an implementation
        /// detail of the tool that produced the file.
        /// </remarks>
        public Timestamp TimeDateStamp => chunk.PeekUInt32(4);

        /// <summary>
        /// The major version number of the debug data format.
        /// </summary>
        public ushort MajorVersion => chunk.PeekUInt16(8);

        /// <summary>
        /// The minor version number of the debug data format.
        /// </summary>
        public ushort MinorVersion => chunk.PeekUInt16(10);

        /// <summary>
        /// The format of debugging information.
        /// </summary>
        public ImageDebugType Type => (ImageDebugType) chunk.PeekUInt32(12);

        /// <summary>
        /// The size of the debug data (not including the debug directory itself).
        /// </summary>
        public int SizeOfData => chunk.PeekInt32(16);

        /// <summary>
        /// The address of the debug data when loaded, relative to the image base.
        /// </summary>
        public int AddressOfRawData => chunk.PeekInt32(20);

        /// <summary>
        /// The file pointer to the debug data.
        /// </summary>
        public int PointerToRawData => chunk.PeekInt32(24);

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
                            case ImageDebugType.Unknown:
                                if (SizeOfData == 0)
                                    return null;

                                goto default;

                            case ImageDebugType.Coff:
                            {
                                data = new ImageCoffSymbolsHeader(valueChunk);
                                break;
                            }

                            case ImageDebugType.CodeView:
                                data = ReadCodeView(valueChunk, SizeOfData);
                                break;

                            case ImageDebugType.FPO:
                            {
                                var numEntries = SizeOfData / FpoData.StructSize;

                                var entries = new FpoData[numEntries];

                                for (var i = 0; i < numEntries; i++)
                                    entries[i] = new FpoData(valueChunk.Slice(i * FpoData.StructSize));

                                data = entries;
                                break;
                            }

                            case ImageDebugType.Misc:
                                data = new ImageDebugMisc(valueChunk);
                                break;

                            case ImageDebugType.Exception:
                                goto default;

                            case ImageDebugType.Fixup:
                            {
                                var entries = new XFixupData[SizeOfData / XFixupData.StructSize];

                                for (var i = 0; i < entries.Length; i++)
                                    entries[i] = new XFixupData(valueChunk.Slice(i * XFixupData.StructSize));

                                data = entries;
                                break;
                            }

                            case ImageDebugType.OmapToSrc: //OMAP type?
                            case ImageDebugType.OmapFromSrc: //OMAP type?
                            case ImageDebugType.Borland:
                                goto default;

                            case ImageDebugType.Reserved10:
                                //C:\Windows\system32\FM20.dll has this with a size of 4. Nobody knows what to do with this directory however
                                //Format seems to be BB 00 and then two more bytes. aspnet_filter.dll had BB 03
                                if (SizeOfData > 0) //Don't know that it can be 0, but good to be defensive
                                    data = new ByteBlob(valueChunk, SizeOfData);
                                else
                                    data = default;
                                break;

                            case ImageDebugType.Clsid:
                                goto default;

                            case ImageDebugType.VCFeature:
                                data = new VCFeature(valueChunk);
                                break;

                            case ImageDebugType.Pogo:
                                //Are they maybe called IMAGE_POGO_BLOCK and IMAGE_POGO_INFO? Need more citations
                                data = ReadPogo(valueChunk, SizeOfData);
                                break;

                            case ImageDebugType.ILTCG:
                                //I've seen this with size 0
                                if (SizeOfData != 0)
                                    goto default;
                                else
                                    data = default;
                                break;

                            case ImageDebugType.MPX:
                                goto default;

                            case ImageDebugType.Reproducible:
                                //dotnet/runtime says that this directory must be empty, but that's not true, it can sometimes have a hash.
                                //it can also sometimes be empty as well
                                if (SizeOfData != 0)
                                    data = new Reproducible(valueChunk);
                                else
                                    data = default;
                                break;

                            case ImageDebugType.EmbeddedPortablePdb:
                                data = new EmbeddedPortablePdb(valueChunk, SizeOfData);
                                break;

                            case ImageDebugType.SPGO:
                                goto default;

                            case ImageDebugType.PdbChecksum:
                                data = new PdbChecksum(valueChunk, SizeOfData);
                                break;

                            case ImageDebugType.ExDllCharacteristics:
                                if (SizeOfData == 4)
                                {
                                    var value = (ImageDllCharacteristicsEx) valueChunk.PeekInt32(0);
                                    data = new RawValue<ImageDllCharacteristicsEx>(valueChunk.AbsoluteOffset, value);
                                }
                                else if (SizeOfData > 0) //Defensively check for 0 length. Has never known to not be 4 bytes
                                    data = new ByteBlob(valueChunk, SizeOfData);
                                else
                                    data = default;
                                break;

                            case ImageDebugType.R2RPerfMap:
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

            switch ((CodeViewSig) chunk.PeekUInt32(0))
            {
                case CodeViewSig.NB10: //PDB v2.0
                    return new NB10I(chunk);

                case CodeViewSig.NB09: //OMF
                case CodeViewSig.NB11:
                    return OMFReader.ReadNB05(chunk, sig, chunk.PeekInt32(sizeOfData - 4), sizeOfData); //The last 4 bytes of the data should be lfoBase, which should be the same value as sizeOfData as well

                case CodeViewSig.RSDS: //PDB v7.0
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
                else if (Data is RawValue<ImageDllCharacteristicsEx> r)
                    writer.WriteGlobal(r.Offset, r.Value, sizeof(int), ViewKind.ExDllCharacteristics);
                else if (Data is FpoData[] f)
                    writer.WriteGlobal(f);
                else
                    throw new NotImplementedException($"Don't know how to write a value of type {Data.GetType().Name}");
            }
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.IMAGE_DEBUG_DIRECTORY, this, ViewKind.ImageDebugDirectory, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(Characteristics), Characteristics);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteField(nameof(Type), Type, sizeof(int));
            s.WriteField(nameof(SizeOfData), SizeOfData);
            s.WriteField(nameof(AddressOfRawData), AddressOfRawData);
            s.WriteField(nameof(PointerToRawData), PointerToRawData);

            return s.ToArray();
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
