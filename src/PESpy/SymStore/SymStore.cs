using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PESpy
{
    internal abstract class SymStore
    {
        public static ValueTask<string?> GetFileAsync(
            ReadOnlySpan<char> searchPath,
            SymStoreKey key,
            SymStoreKey? altKey,
            CancellationToken cancellationToken)
        {
            //Construct a SymStore chain

            SymStore? store = null;

            var run = true;

            while (run)
            {
                var index = searchPath.LastIndexOf('*');

                ReadOnlySpan<char> currentPath;

                if (index == -1)
                {
                    run = false;
                    currentPath = searchPath;
                }
                else
                {
                    currentPath = searchPath.Slice(index + 1);
                    searchPath = searchPath.Slice(0, index);
                }

                if (index == searchPath.Length - 1) //It's the last character
                    throw new NotImplementedException();

                if (currentPath.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase) || currentPath.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    store = new HttpSymStore(new Uri(currentPath.ToString()), store);
                }
                else
                {
                    //Assume it's some kind of filesystem path
                    store = new FileSystemSymStore(currentPath.ToString(), store);
                }
            }

            return CascadeStoreAsync(key, altKey, store, cancellationToken);
        }

        private static async ValueTask<string?> CascadeStoreAsync(SymStoreKey key, SymStoreKey? altKey, SymStore store, CancellationToken cancellationToken)
        {
            var fileAndStream = await store!.CascadeAsync(key, cancellationToken).ConfigureAwait(false);

            if (fileAndStream == null && altKey != null)
                fileAndStream = await store!.CascadeAsync(altKey.Value, cancellationToken).ConfigureAwait(false);

            if (fileAndStream != null)
            {
                fileAndStream.Value.stream.Dispose();
                return fileAndStream.Value.file.FileName;
            }

            return null;
        }

        public SymStore? BackingStore { get; }

        protected SymStore(SymStore? backingStore)
        {
            BackingStore = backingStore;
        }

        public async ValueTask<(SymStoreFile file, Stream stream)?> CascadeAsync(SymStoreKey key, CancellationToken cancellationToken)
        {
            var fileAndStream = await GetFileAsync(key, cancellationToken).ConfigureAwait(false);

            if (fileAndStream == null)
            {
                if (BackingStore != null)
                {
                    fileAndStream = await BackingStore.CascadeAsync(key, cancellationToken).ConfigureAwait(false);

                    if (fileAndStream != null)
                    {
                        var oldStream = fileAndStream.Value.stream;

                        try
                        {
                            fileAndStream = await SaveFileAsync(key, fileAndStream.Value.file, fileAndStream.Value.stream, cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            oldStream.Dispose();
                        }
                    }
                }
            }

            return fileAndStream;
        }

        protected abstract ValueTask<(SymStoreFile file, Stream stream)?> GetFileAsync(SymStoreKey key, CancellationToken cancellationToken);

        protected abstract ValueTask<(SymStoreFile file, Stream stream)?> SaveFileAsync(SymStoreKey key, SymStoreFile file, Stream stream, CancellationToken cancellationToken);
    }
}
