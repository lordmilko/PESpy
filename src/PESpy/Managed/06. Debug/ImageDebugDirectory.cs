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
    public readonly struct ImageDebugDirectory : IValue, IViewable
    {
        /// <summary>
        /// Reserved.
        /// </summary>
        public int Characteristics { get; init; }

        /// <summary>
        /// The time and date that the debug data was created if the PE/COFF file is not deterministic,
        /// otherwise a value based on the hash of the content.
        /// </summary>
        /// <remarks>
        /// The algorithm used to calculate this value is an implementation
        /// detail of the tool that produced the file.
        /// </remarks>
        public uint TimeDateStamp { get; init; }

        /// <summary>
        /// The major version number of the debug data format.
        /// </summary>
        public ushort MajorVersion { get; init; }

        /// <summary>
        /// The minor version number of the debug data format.
        /// </summary>
        public ushort MinorVersion { get; init; }

        /// <summary>
        /// The format of debugging information.
        /// </summary>
        public ImageDebugType Type { get; init; }

        /// <summary>
        /// The size of the debug data (not including the debug directory itself).
        /// </summary>
        public int SizeOfData { get; init; }

        /// <summary>
        /// The address of the debug data when loaded, relative to the image base.
        /// </summary>
        public int AddressOfRawData { get; init; }

        /// <summary>
        /// The file pointer to the debug data.
        /// </summary>
        public int PointerToRawData { get; init; }

        /// <summary>
        /// Gets the data pointed to by this directory.<para/>
        /// This value is not part of the native type definition/
        /// </summary>
        public object? Data { get; }

        public RawOffset Offset { get; }

        internal const int StructSize =
            sizeof(uint) +   // Characteristics
            sizeof(uint) +   // TimeDataStamp
            sizeof(uint) +   // Version
            sizeof(uint) +   // Type
            sizeof(uint) +   // SizeOfData
            sizeof(uint) +   // AddressOfRawData
            sizeof(uint);    // PointerToRawData

        internal ImageDebugDirectory(ref FileReader reader, PEFile peFile)
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

            reader.Seek(offset);

            reader.FillBuffer(SizeOfData);

            switch (Type)
            {
                case ImageDebugType.Unknown:
                case ImageDebugType.Coff:
                    goto default;

                case ImageDebugType.CodeView:
                    Data = ReadCodeView(ref reader, SizeOfData);
                    break;

                case ImageDebugType.FPO:
                {
                    var numEntries = SizeOfData / FpoData.StructSize;

                    var entries = new FpoData[numEntries];

                    for (var i = 0; i < numEntries; i++)
                        entries[i] = new FpoData(ref reader);

                    Data = entries;
                    break;
                }

                case ImageDebugType.Misc:
                    Data = new ImageDebugMisc(ref reader);
                    break;

                case ImageDebugType.Exception:
                case ImageDebugType.Fixup:
                case ImageDebugType.OmapToSrc: //OMAP type?
                case ImageDebugType.OmapFromSrc: //OMAP type?
                case ImageDebugType.Borland:
                    goto default;

                case ImageDebugType.Reserved10:
                    //C:\Windows\system32\FM20.dll has this with a size of 4. Nobody knows what to do with this directory however
                    //Format seems to be BB 00 and then two more bytes. Not sure if the 00 is always 00?
                    if (SizeOfData > 0) //Don't know that it can be 0, but good to be defensive
                        Data = new ByteBlob(ref reader, SizeOfData);
                    else
                        Data = default;
                    break;

                case ImageDebugType.Clsid:
                    goto default;

                case ImageDebugType.VCFeature:
                    Data = new VCFeature(ref reader);
                    break;

                case ImageDebugType.Pogo:
                    //Are they maybe called IMAGE_POGO_BLOCK and IMAGE_POGO_INFO? Need more citations
                    Data = ReadPogo(ref reader, SizeOfData);
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
                        Data = new Reproducible(ref reader);
                    else
                        Data = default;
                    break;

                case ImageDebugType.EmbeddedPortablePdb:
                    Data = new EmbeddedPortablePdb(ref reader, SizeOfData);
                    break;

                case ImageDebugType.SPGO:
                    goto default;

                case ImageDebugType.PdbChecksum:
                    Data = new PdbChecksum(ref reader, SizeOfData);
                    break;

                case ImageDebugType.ExDllCharacteristics:
                    if (SizeOfData == 4)
                    {
                        var value = (ImageDllCharacteristicsEx) reader.ReadInt32();
                        Data = new RawValue<ImageDllCharacteristicsEx>((RawOffset) offset, value);
                    }
                    else if (SizeOfData > 0) //Defensively check for 0 length. Has never known to not be 4 bytes
                        Data = new ByteBlob(ref reader, SizeOfData);
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
                        Data = new ByteBlob(ref reader, SizeOfData);
                    else
                        Data = default;
                    break;
            }

            #endregion
        }

        #region Data

        private static IValue ReadCodeView(ref FileReader reader, int sizeOfData)
        {
            var signature = reader.ReadInt32();

            switch (signature)
            {
                case RSDSI.RSDSSignature:
                    return new RSDSI(ref reader, signature);

                case NB10I.NB10Signature:
                    return new NB10I(ref reader, signature);

                default:
                    Debug.Assert(false, $"Don't know how to read CodeView signature '{signature:X}'");

                    //Unsupported value; read as a byte blob
                    return new ByteBlob(ref reader, sizeOfData);
            }
        }

        private static IValue ReadPogo(ref FileReader reader, int sizeOfData)
        {
            var signature = reader.ReadInt32();

            switch (signature)
            {
                case PogoData.ZeroSignature:
                case PogoData.LCTGSignature:
                case PogoData.PGISignature:
                case PogoData.PGOSignature:
                case PogoData.PGUSignature:
                case PogoData.SPGOSignature:
                    return new PogoData(ref reader, signature, sizeOfData);

                default:
                    Debug.Assert(false, $"Don't know how to read Pogo signature '{signature:X}'");

                    //Unsupported value; read as a byte blob
                    return new ByteBlob(ref reader, sizeOfData);
            }
        }

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
                    writer.WriteGlobal(r.Offset, r, sizeof(int), ViewKind.ExDllCharacteristics);
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
