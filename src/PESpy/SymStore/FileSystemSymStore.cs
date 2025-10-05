using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PESpy
{
    class FileSystemSymStore : SymStore
    {
        public string DirectoryName { get; }

        public FileSystemSymStore(string directoryName, SymStore? backingStore) : base(backingStore)
        {
            DirectoryName = directoryName;
        }

        protected override ValueTask<(SymStoreFile file, Stream stream)?> GetFileAsync(SymStoreKey key, CancellationToken cancellationToken)
        {
            var fileName = Path.Combine(DirectoryName, key.Index);

            if (File.Exists(fileName))
            {
                var fs = File.OpenRead(fileName);

                //If the length is 0, assume that we previously attempted to download the file and that it got corrupt.
                //Try and download the file again
                if (fs.Length == 0)
                {
                    fs.Dispose();
                    return default;
                }

                return new ValueTask<(SymStoreFile file, Stream stream)?>((new SymStoreFile(fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)), fs));
            }

            return default;
        }

        protected override async ValueTask<(SymStoreFile file, Stream stream)?> SaveFileAsync(SymStoreKey key, SymStoreFile file, Stream stream, CancellationToken cancellationToken)
        {
            var fileName = Path.Combine(DirectoryName, key.Index);

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            //We don't want to download to the final file, because if the download is interrupted we'll trip over the file
            //when we next attempt to read the file, since it will now already exist

            //Per symsrv!GetTempDownloadFIleName
            var tempFile = Path.Combine(DirectoryName, $"download{Guid.NewGuid().ToString("N").ToUpperInvariant()}.error");

            using (var fs = File.OpenWrite(tempFile))
            {
                await stream.CopyToAsync(fs, 81920, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFile, fileName);

            return (new SymStoreFile(fileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)), File.OpenRead(fileName));
        }
    }
}
