using System;

namespace PESpy
{
    class HttpSymStore : SymStore
    {
        public Uri Uri { get; }

        public HttpSymStore(Uri uri, SymStore? backingStore) : base(backingStore)
        {
            Uri = uri;
        }

        protected override SymStoreFile? GetFile(SymStoreKey key)
        {
            throw new NotImplementedException();
        }

        protected override void SaveFile(SymStoreKey key, SymStoreFile file)
        {
            throw new NotImplementedException();
        }
    }
}
