using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PESpy
{
    class HttpSymStore : SymStore
    {
        public Uri Uri { get; }

        //We don't want to be constructing a new HttpClient for each request
        private static HttpClient? client;

        private static HttpClient Client
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

        protected override ValueTask<(SymStoreFile file, Stream stream)?> GetFileAsync(SymStoreKey key, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(Uri, key.Index, out var requestUri))
                throw new NotImplementedException();

            return GetWithProgressAsync(requestUri, cancellationToken);
        }

        private async ValueTask<(SymStoreFile file, Stream stream)?> GetWithProgressAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            //We can't dispose the response immediately if we want to later read the stream
            var response = await Client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            bool dispose = true;

            try
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var length = response.Content.Headers.ContentLength;

                    if (length == null)
                        return default;

                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    //Transfer ownership of the response to the progress stream
                    dispose = false;

                    return new(new SymStoreFile(requestUri.AbsoluteUri), new HttpProgressStream(response, stream, length.Value));
                }

                return default; //temp
            }
            finally
            {
                if (dispose)
                    response.Dispose();
            }
            
        }

        protected override ValueTask<(SymStoreFile file, Stream stream)?> SaveFileAsync(SymStoreKey key, SymStoreFile file, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
