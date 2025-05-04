using ClrDebug;
using PESpy.Native;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="STORAGEHEADER"/> type that is pointed to by <see cref="IMAGE_COR20_HEADER.MetaData"/>.<para/>
    /// This type immediately follows the <see cref="STORAGESIGNATURE"/> (which has a variable length due to the presence of the version string).
    /// </summary>
    public readonly struct StorageHeader : IValue, IViewable
    {
#if PEFAST
        public STGHDR Flags => (STGHDR) chunk.PeekByte(0);

        public byte Padding => chunk.PeekByte(1);

        public short Streams => chunk.PeekInt16(2);

        public StorageStream[] StreamHeaders { get; }
#else
        public STGHDR Flags { get; init; }

        public byte Padding { get; init; }

        public short Streams { get; init; }

        public StorageStream[] StreamHeaders { get; init; }
#endif

#if PEFAST
        public RawOffset Offset => chunk.AbsoluteOffset;
#else
        public RawOffset Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal StorageHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            StreamHeaders = null;

            var streamHeaders = new StorageStream[Streams];

            var read = 4;

            for (var i = 0; i < Streams; i++)
            {
                var stream = new StorageStream(chunk.Slice(read));
                streamHeaders[i] = stream;
                read += (StorageStream.FixedStructSize + stream.Name.Length + 1 + 3) & ~3;
            }

            StreamHeaders = streamHeaders;
        }
#else
        internal StorageHeader(
            IFileReader reader,
            IMetadataCallback callback,
            RawOffset metadataRootOffset)
        {
            Offset = (RawOffset) reader.Position;

            Flags = (STGHDR) reader.ReadByte();
            Padding = reader.ReadByte();

            Streams = reader.ReadInt16();

            var streamHeaders = new StorageStream[Streams];

            for (var i = 0; i < Streams; i++)
                streamHeaders[i] = new StorageStream(reader, callback, metadataRootOffset);

            StreamHeaders = streamHeaders;
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(STORAGEHEADER), this, ViewKind.StorageHeader);

            s.WriteField("fFlags", Flags, sizeof(byte));
            s.WriteField("pad", Padding);
            s.WriteField("iStreams", Streams);
            s.WriteInline(StreamHeaders);
        }
    }
}
