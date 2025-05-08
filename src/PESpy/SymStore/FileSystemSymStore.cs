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

        protected override SymStoreFile? GetFile(SymStoreKey key)
        {
            var fileName = Path.Combine(DirectoryName, key.Index);

            if (File.Exists(fileName))
            {
                using var fs = File.OpenRead(fileName);

                //If the length is 0, assume that we previously attempted to download the file and that it got corrupt.
                //Try and download the file again
                if (fs.Length == 0)
                    return null;

                return new SymStoreFile(fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            }

            return null;
        }

        protected override void SaveFile(SymStoreKey key, SymStoreFile file)
        {
            throw new System.NotImplementedException();
        }
    }
}
