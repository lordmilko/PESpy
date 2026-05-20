using System;
using System.IO;
using static PESpy.NativeMethods;

namespace PESpy
{
    internal class WinHttpResponseStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        private int _length;
        private SafeWinHttpHandle _hRequest;
        private int totalRead;
        private ILocatorProgress? _progress;

        //Length should be -1 if not available
        internal WinHttpResponseStream(SafeWinHttpHandle hRequest, int length, ILocatorProgress? progress)
        {
            _hRequest = hRequest;
            _length = length;
            _progress = progress;
        }

        public override void Flush() => throw new NotSupportedException();

        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            int bytesAvailable;

            if (WinHttpQueryDataAvailable(_hRequest, &bytesAvailable) == 0)
                return 0;

            var numBytesToRead = Math.Min(bytesAvailable, count);

            int numBytesRead;

            fixed (byte* pBuffer = buffer)
            {
                var result = WinHttpReadData(
                    _hRequest,
                    pBuffer,
                    numBytesToRead,
                    &numBytesRead
                );

                if (result == 0)
                    return 0;

                totalRead += numBytesRead;

                var percent = (double) totalRead / _length * 100;

                _progress?.Notify(LocatorProgressEventArgs.CreateCopyCascadeProgress(percent, totalRead, (int) _length));

                return numBytesRead;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (_hRequest != null)
            {
                _hRequest.Dispose();
                _hRequest = default;
            }

            base.Dispose(disposing);
        }
    }
}
