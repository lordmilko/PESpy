using ClrDebug;
using PESpy.Native;
using PESpy.View;

namespace PESpy
{
    /// <summary>
    /// Represents the <see cref="STORAGEHEADER"/> type that is pointed to by <see cref="IMAGE_COR20_HEADER.MetaData"/>.<para/>
    /// This type immediately follows the <see cref="STORAGESIGNATURE"/> (which has a variable length due to the presence of the version string).
    /// </summary>
    public readonly struct StorageHeader : IValue, IViewable
    {
        public STGHDR Flags => (STGHDR) chunk.PeekByte(0);

        public byte Padding => chunk.PeekByte(1);

        public short Streams => chunk.PeekInt16(2);

        public StorageStream[] StreamHeaders { get; }

        public int Offset => chunk.AbsoluteOffset;

        internal const int FixedStructSize =
            sizeof(byte) + //Flags
            sizeof(byte) + //Padding
            sizeof(short); //Streams

        internal int StructSize
        {
            get
            {
                var size = FixedStructSize;

                var streamHeaders = StreamHeaders;

                for (var i = 0; i < streamHeaders.Length; i++)
                    size += streamHeaders[i].StructSize;

                return size;
            }
        }

        private readonly MemoryChunk chunk;

        internal StorageHeader(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            StreamHeaders = null!;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.RelayGlobals(StreamHeaders);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.STORAGEHEADER, this, ViewKind.StorageHeader, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("fFlags", Flags, sizeof(byte));
            s.WriteField("pad", Padding);
            s.WriteField("iStreams", Streams);
            s.WriteInline(StreamHeaders);

            return s.ToArray();
        }
    }
}
