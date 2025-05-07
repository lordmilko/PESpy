namespace PESpy
{
    public static partial class Bundle
    {
        public class Manifest
        {
            public Bundle.HeaderFixed Header { get; }
            public BundleEncodedString BundleID { get; }
            public HeaderFixedV2 AdditionalContext { get; }

            public FileEntry[] Files { get; }

#if PEFAST
            internal Manifest(in MemoryChunk chunk)
            {
                throw new System.NotImplementedException();
            }
#else
            internal Manifest(IFileReader reader)
            {
                Header = new HeaderFixed(reader);
                BundleID = new BundleEncodedString(reader);

                if (Header.MajorVersion >= 2)
                {
                    AdditionalContext = new HeaderFixedV2(reader);
                }

                //Immediately following the headers are file_entry_t records
                var files = new FileEntry[Header.NumEmbeddedFiles];

                for (var i = 0; i < files.Length; i++)
                    files[i] = new FileEntry(reader, Header.MajorVersion);

                Files = files;
            }
#endif
        }
    }    
}
