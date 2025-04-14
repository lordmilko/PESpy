
using ClrDebug;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
using RVA = System.Int32;
#endif

namespace PESpy
{
    public class ImageDataDirectory<T> : IValue, IViewable where T : IValue, IViewable
    {
        public RVA VirtualAddress { get; }

        public int Size { get; }

#if !PEFAST
        private T data;

        public T Data
        {
            get
            {
                if (!peFile.HasRegionFlag(kind))
                {
                    if (peFile.TryGetDirectoryOffset(new ImageDataDirectory { VirtualAddress = VirtualAddress, Size = Size }, out var offset, true))
                        data = peFile.WithReader(offset, createData);

                    peFile.SetRegionFlag(kind);
                }

                return data;
            }
        }
#else
        public T Data => throw new System.NotImplementedException();
#endif

        public RawOffset Offset { get; }

        private PEFile peFile;
        private PERegionKind kind;

#if !PEFAST
        private PEFile.WithReaderCallback<T> createData;

        internal ImageDataDirectory(IFileReader reader, PEFile peFile, PERegionKind kind, PEFile.WithReaderCallback<T> createData)
#else
        internal ImageDataDirectory(IFileReader reader, PEFile peFile, PERegionKind kind)
#endif
        {
            Offset = (RawOffset) reader.Position;

            VirtualAddress = (RVA) reader.ReadInt32();
            Size = reader.ReadInt32();

            this.peFile = peFile;
            this.kind = kind;

#if !PEFAST
            this.createData = createData;
#endif

#if STRESS_TEST
            var oldOffset = reader.Position;
            _ = Data;
            reader.Seek(oldOffset);
#endif
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(IMAGE_DATA_DIRECTORY), this, ViewKind.ImageDataDirectory);

            s.WriteField(nameof(VirtualAddress), (int) VirtualAddress);
            s.WriteField(nameof(Size), Size);

            var value = Data;

            if (value != null)
                writer.WriteGlobal(value);
        }
    }
}
