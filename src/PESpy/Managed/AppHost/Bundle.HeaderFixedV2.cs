namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_v2_t
        public struct HeaderFixedV2
        {
            public Location DepsJsonLocation { get; }

            public Location RuntimeConfigJsonLocation { get; }

            public header_flags_t Flags { get; }

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
        }
    }
}
