using System;
using System.IO;
using System.Net.Http;

namespace PESpy
{
    class HttpSymStore : SymStore
    {
        public Uri Uri { get; }

        private HttpClient? client;

        private HttpClient Client
        {
            get
            {
                //Creating a HttpClient requires loading a bunch of networking stuff from the operating system.
                //Defer doing this unless absolutely necessary
                if (client == null)
                    client = new HttpClient();

                return client;
            }
        }

        public HttpSymStore(Uri uri, SymStore? backingStore) : base(backingStore)
        {
            //Uri.TryCreate drops the end of your base Uri if it does not end in a slash

            if (!uri.AbsoluteUri.EndsWith("/"))
                uri = new Uri(uri.AbsoluteUri + "/");

            Uri = uri;
        }

        protected override SymStoreFile? GetFile(SymStoreKey key)
        {
            throw new NotImplementedException();
        }

        protected override (SymStoreFile file, Stream stream)? SaveFile(SymStoreKey key, SymStoreFile file, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
