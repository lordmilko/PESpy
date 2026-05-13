using System;
using System.Collections.Generic;
using System.Diagnostics;
using PESpy.Native;

namespace PESpy.View.Builder
{
    internal abstract unsafe class ByteViewProvider
    {
        private List<IView> rawBytesResults = new List<IView>();
        private bool isLibFile;
        private readonly FileAccessor _fileAccessor;

        internal ByteViewProvider(FileAccessor fileAccessor, bool isLibFile)
        {
            _fileAccessor = fileAccessor;
            this.isLibFile = isLibFile;
        }

        //Used to get the overlay. Do not use in virtual mode
        //public int FileOrSectionLength => length;
        public abstract long FileOrSectionLength { get; }

        internal IView ReadBlob(long rva, Func<int, int>? getRealOffset, int length)
        {
            var realRVA = getRealOffset == null ? rva : getRealOffset((int) rva);

            var (pBytes, memoryLength, relativeOffset) = AcquireMemory(realRVA);

            var bytes = new NativeSpan<byte>((byte*) pBytes + relativeOffset, length);

            return new ByteBlobView(rva, bytes, null, _fileAccessor); //Auto-detect the kind
        }

        internal IView[]? ReadBytes(ref long currentRVA, long endRVA, ViewKind? kind, Func<int, int>? getRealOffset, Func<int, int>? getRVA, bool isOverlay)
        {
            var offset = currentRVA;

            //We're going to read some data from the target. We need to use the "real" offset, not whatever we're pretending it is
            if (getRealOffset != null)
                offset = getRealOffset((int) offset);

            var (pBytes, memoryLength, relativeOffset) = AcquireMemory(offset);

            Debug.Assert(memoryLength != 0);
            var span = new NativeSpan<byte>((byte*) pBytes, (int) memoryLength);

            Debug.Assert(endRVA > currentRVA);

            if (endRVA <= currentRVA)
                throw new InvalidOperationException("Expected endRVA to be after currentRVA");

            var bytesToRead = (int) (endRVA - currentRVA);

            NativeSpan<byte> bytes;

            if (isOverlay)
            {
                //When reading the overlay from disk, nothing is certain. We can have some level of confidence about the security section,
                //but there could even be data listed after that as well

                if (relativeOffset >= memoryLength)
                    return null;

                bytesToRead = Math.Min(bytesToRead, (int) (memoryLength - relativeOffset));

                bytes = span.Slice((int) relativeOffset, (int) bytesToRead);
            }
            else
            {
                bytes = span.Slice(relativeOffset, bytesToRead);
            }

            IView[]? views;

            if (!TryParseRawBytes(currentRVA, kind, bytes, getRVA, out views))
            {
                if (bytes.Length == 1 && isLibFile && kind == null && bytes[0] == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_PAD)
                    kind = ViewKind.ImageArchivePad;

                var result = new ByteBlobView(currentRVA, bytes, kind, _fileAccessor);
                views = new IView[] { result };
            }

            currentRVA += bytesToRead - 1;

            return views;
        }

        internal bool TryParseRawBytes(long offset, ViewKind? kind, NativeSpan<byte> bytes, Func<int, int>? getRVA, out IView[]? views)
        {
            //Try get code first, then strings

            rawBytesResults.Clear();

            if (bytes.Length >= StringParser.MinimumStringLength || rawBytesResults.Count > 0) //If we've already read some assembly code, force processing
            {
                var strs = StringParser.GetStrings((byte*) bytes, bytes.Length);

                if (strs.Length > 0 || rawBytesResults.Count > 0)
                {
                    SplitBytes(offset, bytes, strs, rawBytesResults, kind);

                    //Don't attempt to create Strings regions. Merger may already have an existing repeating group going on.
                    //Leave it to them to merge everything in
                    views = rawBytesResults.ToArray();
                    rawBytesResults.Clear();
                    return true;
                }
            }

            views = null;
            return false;
        }

        private void SplitBytes(long offset, NativeSpan<byte> bytes, ExtractedString[] strs, List<IView> results, ViewKind? kind)
        {
            var strIndex = 0;

            for (var i = 0; i < bytes.Length; i++)
            {
                if (strIndex < strs.Length)
                {
                    var nextValue = strs[strIndex];

                    if (nextValue.Start == i)
                    {
                        if (nextValue.IsUnicode)
                            results.Add(new ValueView<FixedUtf16String>(offset + nextValue.Start, nextValue.Unicode, nextValue.Length, ViewKind.Utf16String, _fileAccessor));
                        else
                            results.Add(new ValueView<AnsiString>(offset + nextValue.Start, nextValue.Ansi, nextValue.Length, ViewKind.AnsiString, _fileAccessor));

                        i += nextValue.Length - 1;
                        strIndex++;
                    }
                    else
                    {
                        //Read all bytes up to the next metadata item
                        results.Add(CreateByteBlob(offset, ref i, kind, nextValue.Start, bytes));
                    }
                }
                else
                {
                    //Read all bytes to the end
                    results.Add(CreateByteBlob(offset, ref i, kind, bytes.Length, bytes));
                }
            }
        }

        ByteBlobView CreateByteBlob(long offset, ref int i, ViewKind? localKind, int end, NativeSpan<byte> bytes)
        {
            var arr = bytes.Slice(i, end - i);

            //If we have a name, but all of the bytes in this section are 0, it's now padding (e.g. after the DOS Stub)
            if (localKind != null && arr.All(b => b == 0))
                localKind = null;

            var blob = new ByteBlobView(offset + i, arr, localKind, _fileAccessor);
            i += arr.Length - 1;
            return blob;
        }

        protected abstract unsafe (IntPtr pBytes, long memoryLength, int relativeOffset) AcquireMemory(long targetAddress);
    }
}
