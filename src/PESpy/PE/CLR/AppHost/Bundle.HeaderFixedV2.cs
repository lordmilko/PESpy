using System;

namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_v2_t
        public struct HeaderFixedV2
        {
#if PEFAST
            public Location DepsJsonLocation => new Location(chunk);

            public Location RuntimeConfigJsonLocation => new Location(chunk.Slice(Location.StructSize));

            public header_flags_t Flags => (header_flags_t) chunk.PeekUInt64(Location.StructSize * 2);

            private FixedUtf8String depsJson;

            public FixedUtf8String DepsJson
            {
                get
                {
                    if (depsJson.Equals(null))
                    {
                        var location = DepsJsonLocation;

                        //I think it's an absolute offset?
                    }

                    throw new NotImplementedException();
                }
            }

            private FixedUtf8String runtimeConfigJson;

            public FixedUtf8String RuntimeConfigJson
            {
                get
                {
                    if (runtimeConfigJson.Equals(null))
                    {
                        var location = RuntimeConfigJsonLocation;

                        //I think it's an absolute offset?
                    }

                    throw new NotImplementedException();
                }
            }
#else
            public Location DepsJsonLocation { get; }

            public Location RuntimeConfigJsonLocation { get; }

            public header_flags_t Flags { get; }
#endif

#if PEFAST
            private readonly MemoryChunk chunk;

            internal HeaderFixedV2(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                depsJson = default;
                runtimeConfigJson = default;
            }
#else
            internal HeaderFixedV2(IFileReader reader)
            {
                DepsJsonLocation = new Location(reader);
                RuntimeConfigJsonLocation = new Location(reader);
                Flags = (header_flags_t) reader.ReadInt64();

                var oldPosition = reader.Position;

                reader.Seek(DepsJsonLocation.Offset);
                var depsJson = reader.ReadUTF8String((int) DepsJsonLocation.Size);

                reader.Seek(RuntimeConfigJsonLocation.Offset);
                var runtimeConfigJson = reader.ReadUTF8String((int) RuntimeConfigJsonLocation.Size);

                reader.Seek(oldPosition);
            }
#endif
        }
    }
}
