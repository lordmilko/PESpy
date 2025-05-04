using System;
using ClrDebug;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="STORAGESIGNATURE"/> type that is pointed to by <see cref="IMAGE_COR20_HEADER.MetaData"/>.
    /// </summary>
    public readonly struct StorageSignature : IValue, IViewable
    {
        private const uint STORAGE_MAGIC_SIG = 0x424A5342; //BSJB

#if PEFAST
        /// <summary>
        /// Magic signature for physical metadata : 0x424A5342.
        /// </summary>
        public uint Signature => chunk.PeekUInt32(0);

        public short MajorVersion => chunk.PeekInt16(4);

        public short MinorVersion => chunk.PeekInt16(6);

        public int ExtraData => chunk.PeekInt32(8);

        public int VersionStringLength => chunk.PeekInt32(12);

        public FixedUtf8String Version => chunk.PeekUtf8FixedLength(16, VersionStringLength);
#else
        /// <summary>
        /// Magic signature for physical metadata : 0x424A5342.
        /// </summary>
        public uint Signature { get; init; }

        public short MajorVersion { get; init; }

        public short MinorVersion { get; init; }

        public int ExtraData { get; init; }

        public int VersionStringLength { get; init; }

        public string Version { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

        internal const int FixedStructSize =
            sizeof(uint) + //Signature
            sizeof(short) + //MajorVersion
            sizeof(short) + //MinorVersion
            sizeof(int) + //ExtraData
            sizeof(int); //VersionStringLength

#if PEFAST
        private readonly MemoryChunk chunk;

        internal StorageSignature(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
#else
        internal StorageSignature(IFileReader reader)
        {
            Offset = (RawOffset) reader.Position;

            Signature = reader.ReadUInt32();

            if (Signature != STORAGE_MAGIC_SIG)
                throw new NotImplementedException("Don't know how to handle storage signature being incorrect");

            MajorVersion = reader.ReadInt16();
            MinorVersion = reader.ReadInt16();
            ExtraData = reader.ReadInt32();

            VersionStringLength = reader.ReadInt32();

            //It's not null-padded, but this function is fine
            Version = reader.ReadNullPaddedUTF8(VersionStringLength);

            //Align to next 4 byte boundary
            var alignmentTarget = (reader.Position + 3) & ~3;
            reader.Seek((reader.Position + 3) & ~3);
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(STORAGESIGNATURE), this, ViewKind.StorageSignature);

            s.WriteField("iSignature", Signature);
            s.WriteField("iMajorVer", MajorVersion);
            s.WriteField("iMinorVer", MinorVersion);
            s.WriteField("iExtraData", ExtraData);
            s.WriteField("iVersionString", VersionStringLength);
            s.WriteUtf8FixedLengthField("pVersion", Version);

            s.Align(4);
        }
    }
}
