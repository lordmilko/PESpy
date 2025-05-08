using System;

namespace PESpy
{
    abstract class SymStore
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

            var file = store!.Cascade(key);

            if (file != null)
            {
                filePath = file.Value.FileName;
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

        public SymStoreFile? Cascade(SymStoreKey key)
        {
            var file = GetFile(key);

            if (file == null)
            {
                if (BackingStore != null)
                {
                    file = BackingStore.Cascade(key);

                    if (file != null)
                        SaveFile(key, file.Value);
                }
            }

            return file;
        }

        protected abstract SymStoreFile? GetFile(SymStoreKey key);

        protected abstract void SaveFile(SymStoreKey key, SymStoreFile file);
    }
}
