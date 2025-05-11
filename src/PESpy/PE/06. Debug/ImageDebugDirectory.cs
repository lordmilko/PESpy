using System;
using System.Diagnostics;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents one of several <see cref="IMAGE_DEBUG_DIRECTORY"/> entries that may be pointed to by IMAGE_OPTIONAL_HEADER.DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].
    /// </summary>
    public struct ImageDebugDirectory : IValue, IViewable
    {
        /// <summary>
        /// Reserved.
        /// </summary>
#if PEFAST
        public int Characteristics => chunk.PeekInt32(0);
#else
        public int Characteristics { get; init; }
#endif

        /// <summary>
        /// The time and date that the debug data was created if the PE/COFF file is not deterministic,
        /// otherwise a value based on the hash of the content.
        /// </summary>
        /// <remarks>
        /// The algorithm used to calculate this value is an implementation
        /// detail of the tool that produced the file.
        /// </remarks>
#if PEFAST
        public uint TimeDateStamp => chunk.PeekUInt32(4);
#else
        public uint TimeDateStamp { get; init; }
#endif

        /// <summary>
        /// The major version number of the debug data format.
        /// </summary>
#if PEFAST
        public ushort MajorVersion => chunk.PeekUInt16(8);
#else
        public ushort MajorVersion { get; init; }
#endif

        /// <summary>
        /// The minor version number of the debug data format.
        /// </summary>
#if PEFAST
        public ushort MinorVersion => chunk.PeekUInt16(10);
#else
        public ushort MinorVersion { get; init; }
#endif

        /// <summary>
        /// The format of debugging information.
        /// </summary>
#if PEFAST
        public ImageDebugType Type => (ImageDebugType) chunk.PeekUInt32(12);
#else
        public ImageDebugType Type { get; init; }
#endif

        /// <summary>
        /// The size of the debug data (not including the debug directory itself).
        /// </summary>
#if PEFAST
        public int SizeOfData => chunk.PeekInt32(16);
#else
        public int SizeOfData { get; init; }
#endif

        /// <summary>
        /// The address of the debug data when loaded, relative to the image base.
        /// </summary>
#if PEFAST
        public int AddressOfRawData => chunk.PeekInt32(20);
#else
        public int AddressOfRawData { get; init; }
#endif

        /// <summary>
        /// The file pointer to the debug data.
        /// </summary>
#if PEFAST
        public int PointerToRawData => chunk.PeekInt32(24);
#else
        public int PointerToRawData { get; init; }
#endif

        /// <summary>
        /// Gets the data pointed to by this directory.<para/>
        /// This value is not part of the native type definition.
        /// </summary>
#if PEFAST
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
                                //Format seems to be BB 00 and then two more bytes. Not sure if the 00 is always 00?
                                if (SizeOfData > 0) //Don't know that it can be 0, but good to be defensive
                                    //data = new ByteBlob(valueChunk, SizeOfData);
                                    throw new NotImplementedException();
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
                                    data = new RawValue<ImageDllCharacteristicsEx>((RawOffset) valueChunk.AbsoluteOffset, value);
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
#else
        public object? Data { get; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int StructSize =
            sizeof(uint) +   // Characteristics
            sizeof(uint) +   // TimeDataStamp
            sizeof(uint) +   // Version
            sizeof(uint) +   // Type
            sizeof(uint) +   // SizeOfData
            sizeof(uint) +   // AddressOfRawData
            sizeof(uint);    // PointerToRawData

#if PEFAST
        private readonly MemoryChunk chunk;

        internal ImageDebugDirectory(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            data = default;

#if STRESS_TEST
            _ = Data;
#endif
        }
#else
        internal ImageDebugDirectory(IFileReader reader, PEFile peFile)
        {
            Offset = (RawOffset) reader.Position;

            reader.FillBuffer(StructSize);

            Characteristics = reader.ReadInt32();

#if DEBUG
            //Only check this in debug, as a malicious PE file might have garbage here to try and trip us up
            if (Characteristics != 0)
                throw new BadImageFormatException($"The value of field {nameof(Characteristics)} in debug directory entry must be zero.");
#endif

            TimeDateStamp = reader.ReadUInt32();
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();

            Type = (ImageDebugType) reader.ReadInt32();

            SizeOfData = reader.ReadInt32();
            AddressOfRawData = reader.ReadInt32();
            PointerToRawData = reader.ReadInt32();

            #region Data

#if RELEASE
            //Ensure that we don't accidentally create a ByteBlob around a buffer of 0 length

            if (SizeOfData == 0)
            {
                Data = default;
                return;
            }
#endif

            var offset = peFile.IsLoadedImage ? AddressOfRawData : PointerToRawData;

            //PointerToRawData can be a bogus negative value

            if (PointerToRawData < 0)
            {
                Data = default;
                return;
            }

            reader.Seek(offset);

            reader.FillBuffer(SizeOfData);

            switch (Type)
            {
                case ImageDebugType.Unknown:
                    if (SizeOfData == 0)
                    {
                        Data = default;
                        return;
                    }

                    goto default;

                case ImageDebugType.Coff:
                {
                    Data = new ImageCoffSymbolsHeader(reader, peFile);
                    break;
                }

                case ImageDebugType.CodeView:
                    Data = ReadCodeView(reader, SizeOfData);
                    break;

                case ImageDebugType.FPO:
                {
                    var numEntries = SizeOfData / FpoData.StructSize;

                    var entries = new FpoData[numEntries];

                    for (var i = 0; i < numEntries; i++)
                        entries[i] = new FpoData(reader);

                    Data = entries;
                    break;
                }

                case ImageDebugType.Misc:
                    Data = new ImageDebugMisc(reader);
                    break;

                case ImageDebugType.Exception:
                    goto default;

                case ImageDebugType.Fixup:
                {
                    var entries = new XFixupData[SizeOfData / XFixupData.StructSize];

                    for (var i = 0; i < entries.Length; i++)
                        entries[i] = new XFixupData(reader);

                    Data = entries;
                    break;
                }

                case ImageDebugType.OmapToSrc: //OMAP type?
                case ImageDebugType.OmapFromSrc: //OMAP type?
                case ImageDebugType.Borland:
                    goto default;

                case ImageDebugType.Reserved10:
                    //C:\Windows\system32\FM20.dll has this with a size of 4. Nobody knows what to do with this directory however
                    //Format seems to be BB 00 and then two more bytes. Not sure if the 00 is always 00?
                    if (SizeOfData > 0) //Don't know that it can be 0, but good to be defensive
                        Data = new ByteBlob(reader, SizeOfData);
                    else
                        Data = default;
                    break;

                case ImageDebugType.Clsid:
                    goto default;

                case ImageDebugType.VCFeature:
                    Data = new VCFeature(reader);
                    break;

                case ImageDebugType.Pogo:
                    //Are they maybe called IMAGE_POGO_BLOCK and IMAGE_POGO_INFO? Need more citations
                    Data = ReadPogo(reader, SizeOfData);
                    break;

                case ImageDebugType.ILTCG:
                    //I've seen this with size 0
                    if (SizeOfData != 0)
                        goto default;
                    else
                        Data = default;
                    break;

                case ImageDebugType.MPX:
                    goto default;

                case ImageDebugType.Reproducible:
                    //dotnet/runtime says that this directory must be empty, but that's not true, it can sometimes have a hash.
                    //it can also sometimes be empty as well
                    if (SizeOfData != 0)
                        Data = new Reproducible(reader);
                    else
                        Data = default;
                    break;

                case ImageDebugType.EmbeddedPortablePdb:
                    Data = new EmbeddedPortablePdb(reader, SizeOfData);
                    break;

                case ImageDebugType.SPGO:
                    goto default;

                case ImageDebugType.PdbChecksum:
                    Data = new PdbChecksum(reader, SizeOfData);
                    break;

                case ImageDebugType.ExDllCharacteristics:
                    if (SizeOfData == 4)
                    {
                        var value = (ImageDllCharacteristicsEx) reader.ReadInt32();
                        Data = new RawValue<ImageDllCharacteristicsEx>((RawOffset) offset, value);
                    }
                    else if (SizeOfData > 0) //Defensively check for 0 length. Has never known to not be 4 bytes
                        Data = new ByteBlob(reader, SizeOfData);
                    else
                        Data = default;
                    break;

                case ImageDebugType.R2RPerfMap:
                default:
#if DEBUG
                    Debug.Assert(false, $"Reading a debug directory of type '{Type}' with size {SizeOfData} is not implemented");
#endif
                    //Defensively check for 0 length
                    if (SizeOfData > 0)
                        Data = new ByteBlob(reader, SizeOfData);
                    else
                        Data = default;
                    break;
            }

            #endregion
        }
#endif

        #region Data

#if PEFAST
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
#else
        private static IValue ReadCodeView(IFileReader reader, int sizeOfData)
        {
            var signature = (CodeViewSig) reader.ReadUInt32();

            switch (signature)
            {
                case CodeViewSig.RSDS:
                    return new RSDSI(reader, signature);

                case CodeViewSig.NB10:
                    return new NB10I(reader, signature);

                default:
                    Debug.Assert(false, $"Don't know how to read CodeView signature '{signature:X}'");

                    //Unsupported value; read as a byte blob
                    return new ByteBlob(reader, sizeOfData);
            }
        }
#endif

#if PEFAST
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
#else
        private static IValue ReadPogo(IFileReader reader, int sizeOfData)
        {
            var signature = (PogoSignatureKind) reader.ReadInt32();

            switch (signature)
            {
                case PogoSignatureKind.Zero:
                case PogoSignatureKind.LCTG:
                case PogoSignatureKind.PGI:
                case PogoSignatureKind.PGO:
                case PogoSignatureKind.PGU:
                case PogoSignatureKind.SPGO:
                    return new PogoData(reader, signature, sizeOfData);

                default:
                    Debug.Assert(false, $"Don't know how to read Pogo signature '{signature:X}'");

                    //Unsupported value; read as a byte blob
                    return new ByteBlob(reader, sizeOfData);
            }
        }
#endif

        #endregion

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_DEBUG_DIRECTORY), this, ViewKind.ImageDebugDirectory);

            s.WriteField(nameof(Characteristics), Characteristics);
            s.WriteField(nameof(TimeDateStamp), TimeDateStamp);
            s.WriteField(nameof(MajorVersion), MajorVersion);
            s.WriteField(nameof(MinorVersion), MinorVersion);
            s.WriteField(nameof(Type), Type, sizeof(int));
            s.WriteField(nameof(SizeOfData), SizeOfData);
            s.WriteField(nameof(AddressOfRawData), AddressOfRawData);
            s.WriteField(nameof(PointerToRawData), PointerToRawData);

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

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}
