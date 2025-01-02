using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View.Builder
{
    class Extension
    {
        private IFileReader reader;

        internal Extension(IFileReader reader)
        {
            this.reader = reader;
        }
            var offset = currentRVA;

            reader.Seek(offset);

            Debug.Assert(endRVA > currentRVA);
            var bytesToRead = endRVA - currentRVA;

            byte[] bytes;

            if (isOverlay)
            {
                //When reading the overlay from disk, nothing is certain. We can have some level of confidence about the security section,
                //but there could even be data listed after that as well
                if (!reader.TryReadBytes((int) bytesToRead, out bytes))
                    return null;
            }
            else
            {
                bytes = reader.ReadBytes((int)bytesToRead);
            }

            IView[] views;

            if (!TryParseRawBytes(currentRVA, kind, bytes, out views))
            {
                var result = new ByteBlobView(currentRVA, bytes, kind);
                views = new IView[] { result };
            }
        internal bool TryParseRawBytes(RawOffset offset, ViewKind? kind, byte[] bytes, out IView[]? views)
        {
            //Try get code first, then strings

            List<IView>? results = null;

            if (kind == ViewKind.DosStub && bytes.Length > 0)
            {
                results = TryParseDosStub(ref offset, ref bytes, kind);
            }

            if (bytes.Length >= StringParser.MinimumStringLength || results != null) //If we've already read some assembly code, force processing
            {
                var strs = StringParser.GetStrings(bytes);

                if (strs.Length > 0 || results != null)
                {
                    if (results == null)
                        results = new List<IView>();

                    SplitBytes(offset, bytes, strs, results, kind);

                    //Don't attempt to create Strings regions. Merger may already have an existing repeating group going on.
                    //Leave it to them to merge everything in
                    views = results.ToArray();
                    return true;
                }
            }

            views = null;
            return false;
        }
        private void SplitBytes(RawOffset offset, byte[] bytes, ExtractedString[] strs, List<IView> results, ViewKind? kind)
        {
            var strIndex = 0;

            for (var i = 0; i < bytes.Length; i++)
            {
                if (strIndex < strs.Length)
                {
                    var nextValue = strs[strIndex];

                    if (nextValue.Start == i)
                    {
                        results.Add(new ValueView<string>(offset + nextValue.Start, nextValue.String, nextValue.Length, ViewKind.String));
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
    }
}
