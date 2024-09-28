using System.Linq;
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
        public STGHDR Flags { get; init; }

        public byte Padding { get; init; }

        public short Streams { get; init; }

        public StorageStream[] StreamHeaders { get; init; }

        public RawOffset Offset { get; }

        internal StorageHeader(
            ref FileReader reader,
            PEFile peFile,
            RawOffset metadataRootOffset)
        {
            Offset = (RawOffset) reader.Position;

            Flags = (STGHDR) reader.ReadByte();
            Padding = reader.ReadByte();

            Streams = reader.ReadInt16();

            var streamHeaders = new StorageStream[Streams];

            for (var i = 0; i < Streams; i++)
                streamHeaders[i] = new StorageStream(ref reader, peFile, metadataRootOffset);

            StreamHeaders = streamHeaders;
        }

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
