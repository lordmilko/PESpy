using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_v2_t
        public class HeaderFixedV2 : IValue, IViewable //May not be present
        {
            private const int DepsJsonLocationOffset = 0;
            private const int RuntimeConfigJsonLocationOffset = Location.StructSize;
            private int FlagsOffset = Location.StructSize * 2;

            public Location DepsJsonLocation => new Location(chunk);

            public Location RuntimeConfigJsonLocation => new Location(chunk.Slice(RuntimeConfigJsonLocationOffset));

            public header_flags_t Flags => (header_flags_t) chunk.PeekUInt64(FlagsOffset);

            private RawValue<FixedUtf8String> depsJson;

            public RawValue<FixedUtf8String> DepsJson
            {
                get
                {
                    if (depsJson.Value.Equals(null))
                    {
                        var location = DepsJsonLocation;

                        if (chunk.PEFile().TryGetValueChunkFromPhysicalOffset((int) location.Offset, out var valueChunk))
                        {
                            depsJson = new RawValue<FixedUtf8String>(valueChunk.AbsoluteOffset, valueChunk.PeekUtf8FixedLength(0, (int) location.Size));
                        }
                    }

                    return depsJson;
                }
            }

            private RawValue<FixedUtf8String> runtimeConfigJson;

            public RawValue<FixedUtf8String> RuntimeConfigJson
            {
                get
                {
                    if (runtimeConfigJson.Value.Equals(null))
                    {
                        var location = RuntimeConfigJsonLocation;

                        if (chunk.PEFile().TryGetValueChunkFromPhysicalOffset((int) location.Offset, out var valueChunk))
                        {
                            runtimeConfigJson = new RawValue<FixedUtf8String>(valueChunk.AbsoluteOffset, valueChunk.PeekUtf8FixedLength(0, (int) location.Size));
                        }
                    }

                    return runtimeConfigJson;
                }
            }

            public int Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                16 + //DepsJsonLocation
                16 + //RuntimeConfigJsonLocation
                sizeof(long); //Flags

            private readonly MemoryChunk chunk;

            internal HeaderFixedV2(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                depsJson = default;
                runtimeConfigJson = default;
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                var depsJson = DepsJson;
                var runtimeConfigJson = RuntimeConfigJson;

                writer.WriteGlobal(depsJson.Offset, depsJson.Value, depsJson.Value.Length, ViewKind.DepsJson);
                writer.WriteGlobal(runtimeConfigJson.Offset, runtimeConfigJson.Value, runtimeConfigJson.Value.Length, ViewKind.RuntimeConfigJson);
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.header_fixed_v2_t, this, ViewKind.BundleHeaderFixedV2, StructSize);

        int IViewable.NumChildren() => 3;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteStructField("deps_json_location", DepsJsonLocation);
                    break;

                case 1:
                    structWriter.WriteStructField("runtimeconfig_json_location", RuntimeConfigJsonLocation);
                    break;

                case 2:
                    structWriter.WriteField("flags", FlagsOffset, Flags, sizeof(long));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
        }
    }
}
