namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_t
        public struct HeaderFixed
        {
            public int MajorVersion { get; }

            public int MinorVersion { get; }

            public int NumEmbeddedFiles { get; }

            internal HeaderFixed(IFileReader reader)
            {
                MajorVersion = reader.ReadInt32();
                MinorVersion = reader.ReadInt32();
                NumEmbeddedFiles = reader.ReadInt32();
            }
        }
    }
}
