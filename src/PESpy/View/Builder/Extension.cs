using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View.Builder
{
    unsafe class Extension
    {
#if PEFAST
        private byte* mmf;
        private int length;
#else
        private IFileReader reader;
#endif
        private IViewDisassembler? viewDisassembler;
        private List<IView> rawBytesResults = new List<IView>();

#if PEFAST
        internal Extension(byte* mmf, int length, IViewDisassembler? viewDisassembler)
        {
            this.mmf = mmf;
            this.length = length;
            this.viewDisassembler = viewDisassembler;
        }

        internal long GetInputLength() => length;
#else
        internal Extension(IFileReader reader, IViewDisassembler? viewDisassembler)
        {
            this.reader = reader;
            this.viewDisassembler = viewDisassembler;
        }

        internal long GetInputLength() => ((StreamFileReader) reader).GetStreamUnsafe().Length;
#endif

        internal IView[]? ReadBytes(ref RawOffset currentRVA, RawOffset endRVA, ViewKind? kind, Func<int, int>? getRealOffset, Func<int, int>? getRVA, bool isOverlay)
        {
            var offset = currentRVA;

            //We're going to read some data from the target. We need to use the "real" offset, not whatever we're pretending it is
            if (getRealOffset != null)
                offset = getRealOffset(offset);

#if PEFAST
            var span = new NativeSpan<byte>(mmf, length);
#else
            reader.Seek(offset);
#endif

            Debug.Assert(endRVA > currentRVA);

            if (endRVA <= currentRVA)
                throw new InvalidOperationException("Expected endRVA to be after currentRVA");

            var bytesToRead = endRVA - currentRVA;

            NativeSpan<byte> bytes;

            if (isOverlay)
            {
                //When reading the overlay from disk, nothing is certain. We can have some level of confidence about the security section,
                //but there could even be data listed after that as well

#if PEFAST
                if (offset >= length)
                    return null;

                bytesToRead = Math.Min(bytesToRead, length - offset);

                bytes = span.Slice(offset, bytesToRead);
#else
                if (!reader.TryReadBytes((int) bytesToRead, out bytes!))
                    return null;
#endif
            }
            else
            {
#if PEFAST
                bytes = span.Slice(offset, bytesToRead);
#else
                bytes = reader.ReadBytes((int)bytesToRead);
#endif
            }

            IView[]? views;

            if (!TryParseRawBytes(currentRVA, kind, bytes, getRVA, out views))
            {
                var result = new ByteBlobView(currentRVA, bytes, kind);
                views = new IView[] { result };
            }

            currentRVA += bytesToRead - 1;

            return views;
        }

        internal bool TryParseRawBytes(RawOffset offset, ViewKind? kind, NativeSpan<byte> bytes, Func<int, int>? getRVA, out IView[]? views)
        {
            //Try get code first, then strings

            rawBytesResults.Clear();

            if (kind == ViewKind.DosStub)
            {
                if (bytes.Length > 0)
                {
                    if (viewDisassembler != null)
                    {
                        viewDisassembler.TryParseDosStub(ref offset, ref bytes, rawBytesResults);
                    }
                }
            }
            else
            {
                if (getRVA != null) //Known padding does not provide a getRVA
                {
                    //If all bytes are padding, don't ask to parse bytes

                    var isPadding = true;

                    for (var i = 0; i < bytes.Length; i++)
                    {
                        if (bytes[i] != 0)
                        {
                            isPadding = false;
                            break;
                        }
                    }

                    if (!isPadding)
                        viewDisassembler?.TryParseBytes(ref offset, getRVA(offset), ref bytes, rawBytesResults);
                }
            }

            if (bytes.Length >= StringParser.MinimumStringLength || rawBytesResults.Count > 0) //If we've already read some assembly code, force processing
            {
                var strs = StringParser.GetStrings(bytes);

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

        private void SplitBytes(RawOffset offset, NativeSpan<byte> bytes, ExtractedString[] strs, List<IView> results, ViewKind? kind)
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
                            results.Add(new ValueView<Utf16String>(offset + nextValue.Start, nextValue.Unicode, nextValue.Length, ViewKind.String));
                        else
                            results.Add(new ValueView<AnsiString>(offset + nextValue.Start, nextValue.Ansi, nextValue.Length, ViewKind.String));

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

        ByteBlobView CreateByteBlob(RawOffset offset, ref int i, ViewKind? localKind, int end, NativeSpan<byte> bytes)
        {
            var arr = bytes.Slice(i, end - i);

            //If we have a name, but all of the bytes in this section are 0, it's now padding (e.g. after the DOS Stub)
            if (localKind != null && arr.All(b => b == 0))
                localKind = null;

            var blob = new ByteBlobView(offset + i, arr, localKind);
            i += arr.Length - 1;
            return blob;
        }
    }
}
