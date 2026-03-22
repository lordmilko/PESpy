using System;
using System.IO;
using PInvoke;

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

        internal WinHttpResponseStream(SafeWinHttpHandle hRequest, int length)
        {
            _hRequest = hRequest;
            _length = length;
        }

        public override void Flush() => throw new NotSupportedException();

        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            int bytesAvailable;

            if (!WinHttp.WinHttpQueryDataAvailable(_hRequest, &bytesAvailable))
                return 0;

            var numBytesToRead = Math.Min(bytesAvailable, count);

            int numBytesRead;

            fixed (byte* pBuffer = buffer)
            {
                var result = WinHttp.WinHttpReadData(
                    _hRequest,
                    pBuffer,
                    numBytesToRead,
                    &numBytesRead
                );

                if (!result)
                    return 0;

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
