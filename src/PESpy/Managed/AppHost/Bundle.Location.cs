namespace PESpy
{
    public static partial class Bundle
    {
        public struct Location
        {
            public long Offset { get; }
            public long Size { get; }

            internal Location(IFileReader reader)
            {
                Offset = reader.ReadInt64();
                Size = reader.ReadInt64();
            }
        }
    }
}
