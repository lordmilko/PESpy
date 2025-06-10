using System.IO;

namespace PESpy
{
    class FileSystemSymStore : SymStore
    {
        public string DirectoryName { get; }

        public FileSystemSymStore(string directoryName, SymStore? backingStore) : base(backingStore)
        {
            DirectoryName = directoryName;
        }

        protected override (SymStoreFile file, Stream stream)? GetFile(SymStoreKey key)
        {
            var fileName = Path.Combine(DirectoryName, key.Index);

            if (File.Exists(fileName))
            {
                var fs = File.OpenRead(fileName);

                //If the length is 0, assume that we previously attempted to download the file and that it got corrupt.
                //Try and download the file again
                if (fs.Length == 0)
                    return null;

                return (new SymStoreFile(fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)), fs);
            }

            return null;
        }

        protected override (SymStoreFile file, Stream stream)? SaveFile(SymStoreKey key, SymStoreFile file, Stream stream)
        {
            var fileName = Path.Combine(DirectoryName, key.Index);

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            var fs = File.OpenWrite(fileName);

            try
            {
                stream.CopyTo(fs);
            }
            catch
            {
                fs.Dispose();
                throw;
            }

            return (new SymStoreFile(fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)), fs);
        }
    }
}
