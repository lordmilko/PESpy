using System;
using System.IO;

namespace PESpy
{
    internal abstract class SymStore
    {
        public static bool TryGetFile(ReadOnlySpan<char> searchPath, SymStoreKey key, out string? filePath)
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

            var fileAndStream = store!.Cascade(key);

            if (fileAndStream != null)
            {
                fileAndStream.Value.stream.Dispose();
                filePath = fileAndStream.Value.file.FileName;
                return true;
            }

            filePath = default;
            return false;
        }

        public SymStore? BackingStore { get; }

        protected SymStore(SymStore? backingStore)
        {
            BackingStore = backingStore;
        }

        public (SymStoreFile file, Stream stream)? Cascade(SymStoreKey key)
        {
            var fileAndStream = GetFile(key);

            if (fileAndStream == null)
            {
                if (BackingStore != null)
                {
                    fileAndStream = BackingStore.Cascade(key);

                    if (fileAndStream != null)
                    {
                        var oldStream = fileAndStream.Value.stream;

                        try
                        {
                            fileAndStream = SaveFile(key, fileAndStream.Value.file, fileAndStream.Value.stream);
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

        protected abstract (SymStoreFile file, Stream stream)? GetFile(SymStoreKey key);

        protected abstract (SymStoreFile file, Stream stream)? SaveFile(SymStoreKey key, SymStoreFile file, Stream stream);
    }
}
