using System;
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
        private const int FlagsOffset = 0;
        private const int PaddingOffset = 1;
        private const int StreamsOffset = 2;
        public STGHDR Flags => (STGHDR) chunk.PeekByte(FlagsOffset);

        public byte Padding => chunk.PeekByte(PaddingOffset);

        public short Streams => chunk.PeekInt16(StreamsOffset);

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

        int IViewable.NumChildren => 3 + Streams;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("fFlags", FlagsOffset, Flags, sizeof(byte));
                    break;

                case 1:
                    structWriter.WriteField("pad", PaddingOffset, Padding);
                    break;

                case 2:
                    structWriter.WriteField("iStreams", StreamsOffset, Streams);
                    break;

                case 3:
                    structWriter.WriteInline(StreamHeaders[index - 3]);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
