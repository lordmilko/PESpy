using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PESpy
{
    class HttpSymStore : SymStore
    {
        //Don't us the Uri type as it increases our size in NativeAOT
        public string Uri { get; }

#if !NATIVEAOT
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
#endif

        public HttpSymStore(string uri, SymStore? backingStore) : base(backingStore)
        {
            //Uri.TryCreate drops the end of your base Uri if it does not end in a slash.
            //However, we're now doing without Uri entirely to reduce the size used in NativeAOT

            Uri = uri;
        }

#if !NATIVEAOT
        protected override ValueTask<(SymStoreFile file, Stream stream)?> GetFileAsync(SymStoreKey key, ILocatorProgress progress, CancellationToken cancellationToken)
        {
            using var builder = new ValueStringBuilder();
            builder.Append(Uri);

            if (!Uri.EndsWith("/"))
                builder.Append('/');

            builder.Append(key.Index);

            return GetWithProgressAsync(builder.ToString(), progress, cancellationToken);
        }

        private async ValueTask<(SymStoreFile file, Stream stream)?> GetWithProgressAsync(string requestUri, ILocatorProgress progress, CancellationToken cancellationToken)
        {
            progress?.NotifyRequest(requestUri);

            //We can't dispose the response immediately if we want to later read the stream
            var response = await Client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            bool dispose = true;

            try
            {
                progress?.NotifyResponse((int) response.StatusCode);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var length = response.Content.Headers.ContentLength;

                    if (length == null)
                        return default;

                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    //Transfer ownership of the response to the progress stream
                    dispose = false;

                    return new(new SymStoreFile(requestUri), new HttpProgressStream(response, stream, length.Value, progress));
                }

                return default;
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
#endif

        protected override (SymStoreFile file, Stream stream)? GetFile(SymStoreKey key, ILocatorProgress progress, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override (SymStoreFile file, Stream stream)? SaveFile(SymStoreKey key, SymStoreFile file, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
