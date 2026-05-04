using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace PESpy
{
    /// <summary>
    /// Represents a stream that displays progress while reading an inner stream
    /// </summary>
    internal class HttpProgressStream : Stream
    {
        private readonly HttpResponseMessage response;
        private readonly Stream stream;
        private readonly long length;
        private readonly ILocatorProgress? progress;

        public HttpProgressStream(HttpResponseMessage response, Stream stream, long? length, ILocatorProgress? progress)
        {
            this.response = response;
            this.stream = stream;
            this.length = length == null ? -1 : length.Value;
            this.progress = progress;
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => length; //You can't ask for the length from the HTTP response stream

        public override long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        public override void Flush() => stream.Flush();

        private int totalRead;

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = stream.Read(buffer, offset, count);

            totalRead += result;

            var percent = (double) totalRead / length * 100;

            progress?.Notify(LocatorProgressEventArgs.CreateCopyCascadeProgress(percent, totalRead, (int) length));

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);

        public override void SetLength(long value) => stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            stream.Dispose();
            response.Dispose();

            base.Dispose(disposing);
        }
    }
}
