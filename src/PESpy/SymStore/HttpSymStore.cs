using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PInvoke;

namespace PESpy
{
    internal class HttpSymStore : SymStore
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

        public LocatorHttpPolicy HttpPolicy { get; }

        private IFile _file;

        public HttpSymStore(string uri, IFile file, LocatorHttpPolicy httpPolicy, SymStore? backingStore) : base(backingStore, uri)
        {
            //Uri.TryCreate drops the end of your base Uri if it does not end in a slash.
            //However, we're now doing without Uri entirely to reduce the size used in NativeAOT
            Debug.Assert(httpPolicy != LocatorHttpPolicy.Microsoft || file != null);

            Uri = uri;
            HttpPolicy = httpPolicy;
            _file = file;
        }

#if !NATIVEAOT
        protected override ValueTask<(SymStoreFile file, Stream stream)?> GetFileAsync(SymStoreKey key, ILocatorProgress progress, CancellationToken cancellationToken)
        {
            if (!AllowRequest())
                return default;

            using var builder = new ValueStringBuilder();
            builder.Append(Uri);

            if (!Uri.EndsWith("/"))
                builder.Append('/');

            builder.Append(key.Index);

            return GetWithProgressAsync(builder.ToString(), progress, cancellationToken);
        }

        private async ValueTask<(SymStoreFile file, Stream stream)?> GetWithProgressAsync(string requestUri, ILocatorProgress progress, CancellationToken cancellationToken)
        {
            progress?.Notify(LocatorProgressEventArgs.CreateBeginHttpRequest(requestUri));

            //We can't dispose the response immediately if we want to later read the stream
            var response = await Client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            bool dispose = true;

            try
            {
                progress?.Notify(LocatorProgressEventArgs.CreateGotHttpResponse((int) response.StatusCode));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    return default;

                var length = response.Content.Headers.ContentLength;

                if (length == null)
                    return default;

                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                //Transfer ownership of the response to the progress stream
                dispose = false;

                return new(new SymStoreFile(requestUri), new HttpProgressStream(response, stream, length.Value, progress));
            }
            finally
            {
                if (dispose)
                    response.Dispose();
            }
        }

        protected override ValueTask<(SymStoreFile file, Stream stream)?> SaveFileAsync(
            SymStoreKey key,
            SymStoreFile file,
            Stream stream,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
#endif

        internal enum WinHttpResult
        {
            //Negative results are considered fatal; they're not expected to occur.
            //Positive results are expected if we successfully executed the HTTP request,
            //but got a response we can't deal with

            WinHttpAddRequestHeaders = -4,
            WinHttpOpenRequest = -3,
            WinHttpConnect = -2,
            WinHttpCrackUrl = -1,

            Success = 0,

            //Failed to open a session. Soft failure
            WinHttpOpen = 1,

            WinHttpSendRequest = 2,

            //Failed to receive a response. Soft failure
            WinHttpReceiveResponse = 3,

            StatusCodeHeaderMissing = 4,

            BadStatusCode = 5,

            ContentLengthHeaderMissing = 6,
        }

        internal unsafe static WinHttpResult GetFileViaWinHttp(string uri, out Stream stream, out int statusCode) =>
            GetFileViaWinHttp(uri, null, out stream, out statusCode);

        private unsafe static WinHttpResult GetFileViaWinHttp(
            string uri,
            ILocatorProgress? progress,
            out Stream stream,
            out int statusCode)
        {
            BOOL result;

            stream = default;
            statusCode = default;

            char[] urlBuffer = null;
            SafeWinHttpHandle hSession = null;
            SafeWinHttpHandle hConnect = null;
            SafeWinHttpHandle hRequest = null;

            try
            {
                fixed (char* pUri = uri)
                {
                    var components = new URL_COMPONENTS
                    {
                        dwStructSize = sizeof(URL_COMPONENTS),

                        //Don't need dwUserNameLength / dwPasswordLength

                        //For each component, if the length is non-zero, the corresponding buffer of that item will be filled in, and the length
                        //of the associated value will be updated here
                        dwSchemeLength = 1,
                        dwHostNameLength = 1,
                        dwUrlPathLength = 1,
                        dwExtraInfoLength = 1,
                    };

                    if (!WinHttp.WinHttpCrackUrl(pUri, uri.Length, 0, &components))
                        return WinHttpResult.WinHttpCrackUrl;

                    var hostName = new FixedUtf16String(components.lpszHostName, components.dwHostNameLength);
                    var urlPath = new FixedUtf16String(components.lpszUrlPath, components.dwUrlPathLength);

                    hSession = WinHttp.WinHttpOpen(
                        pszAgentW: (PCWSTR) default,
                        dwAccessType: WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, //This is the default setting in WinHttpHandler. AUTOMATIC_PROXY is only supported in Windows 8.1+. DEFAULT_PROXY is deprecated in 8.1 however which is a bit of an issue
                        pszProxyW: null, //WINHTTP_NO_PROXY_NAME is just null
                        pszProxyBypassW: null, //WINHTTP_NO_PROXY_BYPASS is just null
                        dwFlags: 0 //WinHttpHandler specifies WINHTTP_FLAG_ASYNC, but we exclusively want to use this synchronously in NativeAOT
                    );

                    if (hSession == default)
                        return WinHttpResult.WinHttpOpen;

                    urlBuffer = ArrayPool<char>.Shared.Rent(uri.Length + 1);

                    fixed (char* pBuffer = urlBuffer)
                    {
                        hostName.CopyTo(urlBuffer);
                        urlBuffer[hostName.Length] = '\0';

                        //I'm not sure how long I need to keep the pointer to the server name alive, but WinHttpHandler
                        //does not seem to worry about this
                        hConnect = WinHttp.WinHttpConnect(
                            hSession: hSession,
                            pswzServerName: pBuffer,
                            nServerPort: components.nPort,
                            dwReserved: 0
                        );

                        if (hConnect == default)
                            return WinHttpResult.WinHttpConnect;

                        hConnect.Parent = hSession;
                        hSession = null; //Ownership transferred to the connection

                        //WinHttpHandler disables URL escaping, perhaps because System.Uri takes care of that. But we don't use
                        //System.Uri, so we need to include automatic escaping
                        WINHTTP_OPEN_REQUEST_FLAGS requestFlags = 0;

                        if (components.nScheme == WINHTTP_INTERNET_SCHEME.WINHTTP_INTERNET_SCHEME_HTTPS)
                            requestFlags |= WINHTTP_OPEN_REQUEST_FLAGS.WINHTTP_FLAG_SECURE;

                        urlPath.CopyTo(urlBuffer);
                        urlBuffer[urlPath.Length] = '\0';

                        fixed (char* pGet = "GET")
                        {
                            hRequest = WinHttp.WinHttpOpenRequest(
                                hConnect: hConnect,
                                pwszVerb: pGet,
                                pwszObjectName: pBuffer,
                                pwszVersion: null, //Default will be HTTP/1.1
                                pwszReferrer: null, //WINHTTP_NO_REFERER is just null
                                ppwszAcceptTypes: null, //WINHTTP_DEFAULT_ACCEPT_TYPES is just null, also the default behavior of symsrv seems to be to specify null, despite the fact the docs say this means _no_ types are accepted
                                dwFlags: requestFlags
                            );
                        }

                        if (hRequest == default)
                            return WinHttpResult.WinHttpOpenRequest;

                        var headers = "X-TFS-FedAuthRedirect: Suppress";

                        const int WINHTTP_ADDREQ_FLAG_ADD = 0x20000000;

                        //By default, Azure DevOps will respond with a HTTP 203 response, and give you the sign in page. This is not what we want.
                        //symsrv!StoreWinInet::get sets this header unconditionally in all requests. They also set "Accept-Encoding: gzip" but I'm
                        //not sure if that would mean I'd need to add special decompression handling on my end or something
                        result = WinHttp.WinHttpAddRequestHeaders(
                            hRequest: hRequest,
                            lpszHeaders: headers,
                            dwHeadersLength: headers.Length,
                            dwModifiers: WINHTTP_ADDREQ_FLAG_ADD
                        );

                        if (hRequest == default)
                            return WinHttpResult.WinHttpAddRequestHeaders;

                        hRequest.Parent = hConnect;
                        hConnect = null; //Ownership transferred to the request

                        result = WinHttp.WinHttpSendRequest(
                            hRequest: hRequest,
                            lpszHeaders: (PCWSTR) default, //WINHTTP_NO_ADDITIONAL_HEADERS is just null
                            dwHeadersLength: 0,
                            lpOptional: null, //WINHTTP_NO_REQUEST_DATA is just null
                            dwOptionalLength: 0,
                            dwTotalLength: 0,
                            dwContext: default
                        );

                        if (!result)
                            return WinHttpResult.WinHttpSendRequest;

                        result = WinHttp.WinHttpReceiveResponse(hRequest, default);

                        if (!result)
                            return WinHttpResult.WinHttpReceiveResponse;

                        const int WINHTTP_QUERY_CONTENT_LENGTH = 5;
                        const int WINHTTP_QUERY_STATUS_CODE = 19;
                        const int WINHTTP_QUERY_FLAG_NUMBER = 0x20000000;

                        static bool TryGetNumericHeader(void* hRequest, int kind, out int value)
                        {
                            int num;
                            int numSize = sizeof(int);

                            var result = WinHttp.WinHttpQueryHeaders(
                                hRequest: hRequest,
                                dwInfoLevel: kind | WINHTTP_QUERY_FLAG_NUMBER,
                                pwszName: null, //WINHTTP_HEADER_NAME_BY_INDEX is just null
                                lpBuffer: &num,
                                lpdwBufferLength: &numSize,
                                lpdwIndex: default
                            );

                            value = num;

                            return result;
                        }

                        if (!TryGetNumericHeader(hRequest, WINHTTP_QUERY_STATUS_CODE, out statusCode))
                            return WinHttpResult.StatusCodeHeaderMissing;

                        progress?.Notify(LocatorProgressEventArgs.CreateGotHttpResponse((int) statusCode));

                        if (statusCode != 200)
                            return WinHttpResult.BadStatusCode;

                        if (!TryGetNumericHeader(hRequest, WINHTTP_QUERY_CONTENT_LENGTH, out var contentLength))
                            return WinHttpResult.ContentLengthHeaderMissing;

                        stream = new WinHttpResponseStream(hRequest, contentLength, progress);
                        hRequest = null; //Ownership transferred to the response

                        return WinHttpResult.Success;
                    }
                }
            }
            finally
            {
                if (urlBuffer != null)
                    ArrayPool<char>.Shared.Return(urlBuffer);

                if (hRequest != null)
                    hRequest.Dispose();

                if (hConnect != null)
                    hConnect.Dispose();

                if (hSession != null)
                    hSession.Dispose();
            }
        }

        protected unsafe override (SymStoreFile file, Stream stream)? GetFile(
            SymStoreKey key,
            ILocatorProgress progress,
            CancellationToken cancellationToken)
        {
            if (!AllowRequest())
                return default;

            using var builder = new ValueStringBuilder();
            builder.Append(Uri);

            if (!Uri.EndsWith("/"))
                builder.Append('/');

            builder.Append(key.Index);

            var uri = builder.ToString();

            progress?.Notify(LocatorProgressEventArgs.CreateBeginHttpRequest(uri));
            var result = GetFileViaWinHttp(uri, progress, out var stream, out var statusCode);

            if (result < 0)
                throw new HttpRequestException(result.ToString());

            if (result != 0)
                return default;

            return new(new SymStoreFile(uri), stream);
        }

        protected override (SymStoreFile file, Stream stream)? SaveFile(
            SymStoreKey key,
            SymStoreFile file,
            Stream stream,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private bool AllowRequest()
        {
            switch (HttpPolicy)
            {
                case LocatorHttpPolicy.None:
                    return false;

                case LocatorHttpPolicy.Microsoft:
                    if (_file.Kind != FileKind.PE)
                        return true;

                    if (((PEFile) _file).TryGetVersionInfo(out var versionInfo))
                    {
                        foreach (var child in versionInfo.Children)
                        {
                            if (child is VsVersionInfo.StringFileInfo s)
                            {
                                foreach (var stringTable in s.Children)
                                {
                                    foreach (var str in stringTable.Children)
                                    {
                                        if (str.Key == "CompanyName")
                                        {
                                            if (str.Value.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                                                return true;

                                            return false;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //If we can't specifically tell that it's a Microsoft file, assume it isn't
                    return false;

                case LocatorHttpPolicy.All:
                    return true;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
