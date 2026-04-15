using System;
using System.Diagnostics;

namespace PESpy
{
    public enum LocatorProgressEventKind
    {
        BeginHttpRequest,
        GotHttpResponse,

        BeginCascadeStore,
        CopyCascadeFile,
        CopyCascadeProgress,
        EndCascadeStore
    }

    internal class LocatorProgressEventArgsDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
        {
            get
            {
                return _eventArgs.Kind switch
                {
                    LocatorProgressEventKind.BeginHttpRequest => _eventArgs.BeginHttpRequest,
                    LocatorProgressEventKind.GotHttpResponse => _eventArgs.GotHttpResponse,

                    LocatorProgressEventKind.BeginCascadeStore => _eventArgs.BeginCascadeStore,
                    LocatorProgressEventKind.CopyCascadeFile => _eventArgs.CopyCascadeFile,
                    LocatorProgressEventKind.CopyCascadeProgress => _eventArgs.CopyCascadeProgress,
                    LocatorProgressEventKind.EndCascadeStore => _eventArgs.EndCascadeStore
                };
            }
        }

        private LocatorProgressEventArgs _eventArgs;

        internal LocatorProgressEventArgsDebugView(LocatorProgressEventArgs eventArgs)
        {
            _eventArgs = eventArgs;
        }
    }

    [DebuggerTypeProxy(typeof(LocatorProgressEventArgsDebugView))]
    public struct LocatorProgressEventArgs
    {
        public readonly LocatorProgressEventKind Kind;

        #region Create

        public static LocatorProgressEventArgs CreateBeginHttpRequest(string uri) => new LocatorProgressEventArgs(LocatorProgressEventKind.BeginHttpRequest) { _string1 = uri };

        public static LocatorProgressEventArgs CreateGotHttpResponse(int responseCode) => new LocatorProgressEventArgs(LocatorProgressEventKind.GotHttpResponse) { _long1 = responseCode };

        public static LocatorProgressEventArgs CreateBeginCascadeStore(SymStoreKey key, string destinationStore, string sourceStore) =>
            new LocatorProgressEventArgs(LocatorProgressEventKind.BeginCascadeStore) { _key = key, _string1 = destinationStore, _string2 = sourceStore };

        public static LocatorProgressEventArgs CreateCopyCascadeFile(SymStoreKey key, string destinationStore, string sourceStore, int length) =>
            new LocatorProgressEventArgs(LocatorProgressEventKind.CopyCascadeFile) { _key = key, _string1 = destinationStore, _string2 = sourceStore, _long1 = length };

        public static LocatorProgressEventArgs CreateCopyCascadeProgress(double percent, int totalRead, int length) =>
            new LocatorProgressEventArgs(LocatorProgressEventKind.CopyCascadeProgress) { _long1 = BitConverter.DoubleToInt64Bits(percent), _long2 = (((long) totalRead )<< 32) | (long) length };

        public static LocatorProgressEventArgs CreateEndCascadeStore(SymStoreKey key, string destinationStore, string sourceStore) =>
            new LocatorProgressEventArgs(LocatorProgressEventKind.EndCascadeStore) { _key = key, _string1 = destinationStore, _string2 = sourceStore };

        #endregion
        #region Read

        public BeginHttpRequestEventArgs BeginHttpRequest => new(_string1);

        public GotHttpResponseEventArgs GotHttpResponse => new((int) _long1);

        public BeginCascadeStoreEventArgs BeginCascadeStore => new(_key, _string1, _string2);

        public CopyCascadeFileEventArgs CopyCascadeFile => new(_key, _string1, _string2, (int) _long1);

        public CopyCascadeProgressEventArgs CopyCascadeProgress => new(BitConverter.Int64BitsToDouble(_long1), (int) (_long2 >> 32), (int) _long2);

        public EndCascadeStoreEventArgs EndCascadeStore => new(_key, _string1, _string2);

        #endregion

        private SymStoreKey _key;
        private string _string1;
        private string _string2;
        private long _long1;
        private long _long2;

        internal LocatorProgressEventArgs(LocatorProgressEventKind kind)
        {
            Kind = kind;
        }

        public struct BeginHttpRequestEventArgs
        {
            public LocatorProgressEventKind Kind => LocatorProgressEventKind.BeginHttpRequest;

            public string Uri { get; }

            internal BeginHttpRequestEventArgs(string uri)
            {
                Uri = uri;
            }
        }

        public struct GotHttpResponseEventArgs
        {
            public LocatorProgressEventKind Kind => LocatorProgressEventKind.GotHttpResponse;

            public int ResponseCode { get; }

            internal GotHttpResponseEventArgs(int responseCode)
            {
                ResponseCode = responseCode;
            }
        }

        public struct BeginCascadeStoreEventArgs
        {
            public LocatorProgressEventKind Kind => LocatorProgressEventKind.BeginCascadeStore;

            public SymStoreKey Key { get; }

            public string DestinationStore { get; }

            public string SourceStore { get; }

            internal BeginCascadeStoreEventArgs(SymStoreKey key, string destinationStore, string sourceStore)
            {
                Key = key;
                DestinationStore = destinationStore;
                SourceStore = sourceStore;
            }
        }

        public struct CopyCascadeFileEventArgs
        {
            public LocatorProgressEventKind Kind => LocatorProgressEventKind.CopyCascadeFile;

            public SymStoreKey Key { get; }

            public string DestinationStore { get; }

            public string SourceStore { get; }

            public int Length { get; }

            internal CopyCascadeFileEventArgs(SymStoreKey key, string destinationStore, string sourceStore, int length)
            {
                Key = key;
                DestinationStore = destinationStore;
                SourceStore = sourceStore;
                Length = length;
            }
        }

        public struct CopyCascadeProgressEventArgs
        {
            public LocatorProgressEventKind Kind => LocatorProgressEventKind.CopyCascadeProgress;

            public double Percent { get; }

            public int TotalRead { get; }

            public int Length { get; }

            internal CopyCascadeProgressEventArgs(double percent, int totalRead, int length)
            {
                Percent = percent;
                TotalRead = totalRead;
                Length = length;
            }
        }

        public struct EndCascadeStoreEventArgs
        {
            public LocatorProgressEventKind Kind => LocatorProgressEventKind.EndCascadeStore;

            public SymStoreKey Key { get; }

            public string DestinationStore { get; }

            public string SourceStore { get; }

            internal EndCascadeStoreEventArgs(SymStoreKey key, string destinationStore, string sourceStore)
            {
                Key = key;
                DestinationStore = destinationStore;
                SourceStore = sourceStore;
            }
        }
    }
}
