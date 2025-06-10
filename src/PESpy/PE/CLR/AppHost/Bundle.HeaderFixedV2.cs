using System;

namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_v2_t
        public class HeaderFixedV2 //May not be present
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

                        if (chunk.PEFile().TryGetValueChunkFromPhysicalOffset((int) location.Offset, out var valueChunk))
                        {
                            depsJson = valueChunk.PeekUtf8FixedLength(0, (int) location.Size);
                        }
                    }

                    return depsJson;
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

                        if (chunk.PEFile().TryGetValueChunkFromPhysicalOffset((int) location.Offset, out var valueChunk))
                        {
                            runtimeConfigJson = valueChunk.PeekUtf8FixedLength(0, (int) location.Size);
                        }
                    }

                    return runtimeConfigJson;
                }
            }
#else
            public Location DepsJsonLocation { get; }

            public Location RuntimeConfigJsonLocation { get; }

            public header_flags_t Flags { get; }
#endif
            internal const int StructSize =
                16 + //DepsJsonLocation
                16 + //RuntimeConfigJsonLocation
                sizeof(long); //Flags

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
