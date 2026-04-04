using System;
using PESpy.PDB;

namespace PESpy
{
    internal static class AddrHashHelpers
    {
        #region GetFirstSymbol

        internal static void GetFirstSymbol16(
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] offsetTable,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            //Take the first symbol we see?

            for (var i = 0; i < offsetTable.Length; i++)
            {
                var items = offsetTable[i];

                if (items.Length > 0)
                {
                    resultSeg = i + 1;
                    resultOffsetIndex = 0;
                    return;
                }
            }

            throw new NotImplementedException("Don't know how to handle failing to find a first symbol");
        }

        internal static void GetFirstSymbol32(
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] offsetTable,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            //Take the first symbol we see?

            for (var i = 0; i < offsetTable.Length; i++)
            {
                var items = offsetTable[i];

                if (items.Length > 0)
                {
                    resultSeg = i + 1;
                    resultOffsetIndex = 0;
                    return;
                }
            }

            throw new NotImplementedException("Don't know how to handle failing to find a first symbol");
        }

        #endregion
        #region GetLastSymbol

        internal static void GetLastSymbol16(
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] offsetTable,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            for (var i = offsetTable.Length - 1; i >= 0; i--)
            {
                var items = offsetTable[i];

                if (items.Length > 0)
                {
                    resultSeg = i + 1;
                    resultOffsetIndex = items.Length - 1;
                    return;
                }
            }

            throw new NotImplementedException("Don't know how to handle failing to find a last symbol");
        }

        internal static void GetLastSymbol32(
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] offsetTable,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            for (var i = offsetTable.Length - 1; i >= 0; i--)
            {
                var items = offsetTable[i];

                if (items.Length > 0)
                {
                    resultSeg = i + 1;
                    resultOffsetIndex = items.Length - 1;
                    return;
                }
            }

            throw new NotImplementedException("Don't know how to handle failing to find a last symbol");
        }

        #endregion
        #region TryBInarySearchSegmentOffsets

        internal static bool TryBinarySearchSegmentOffsets16(
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)> segmentOffsets,
            int relativeOffset,
            out int symbolOffset,
            out int lo)
        {
            //Try right-biased binary search

            lo = 0;
            var hi = segmentOffsets.Length - 1;

            while (lo < hi)
            {
                var mid = lo + ((hi - lo + 1) / 2);

                if (segmentOffsets[mid].sectionRelativeOffset <= relativeOffset)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            if (segmentOffsets.Length > 0 && segmentOffsets[lo].sectionRelativeOffset <= relativeOffset)
            {
                symbolOffset = segmentOffsets[lo].symbolOffset;
                return true;
            }

            symbolOffset = default;
            return false;
        }

        internal static bool TryBinarySearchSegmentOffsets32(
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)> segmentOffsets,
            int relativeOffset,
            out int symbolOffset,
            out int lo)
        {
            //Try right-biased binary search

            lo = 0;
            var hi = segmentOffsets.Length - 1;

            while (lo < hi)
            {
                var mid = lo + ((hi - lo + 1) / 2);

                if (segmentOffsets[mid].sectionRelativeOffset <= relativeOffset)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            if (segmentOffsets.Length > 0 && segmentOffsets[lo].sectionRelativeOffset <= relativeOffset)
            {
                symbolOffset = segmentOffsets[lo].symbolOffset;
                return true;
            }

            symbolOffset = default;
            return false;
        }

        #endregion
        #region TryLinearSearchOffsetTable

        internal static bool TryLinearSearchOffsetTable16(
            NativeSpan<(ushort symbolOffset, ushort sectionRelativeOffset)>[] offsetTable,
            ushort sectionNumber,
            out int symbolOffset,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            //Section number is 1-based, and we want the previous one to that
            //so do -2
            for (var i = sectionNumber - 2; i >= 0; i--)
            {
                var offsets = offsetTable[i];

                if (offsets.Length > 0)
                {
                    //Use the last symbol of the previous segment
                    var last = offsets[offsets.Length - 1];
                    symbolOffset = last.symbolOffset;
                    resultOffsetIndex = offsets.Length - 1;
                    resultSeg = i + 1;
                    return true;
                }
            }

            symbolOffset = default;
            resultSeg = default;
            resultOffsetIndex = default;
            return false;
        }

        internal static bool TryLinearSearchOffsetTable32(
            NativeSpan<(int symbolOffset, int sectionRelativeOffset)>[] offsetTable,
            ushort sectionNumber,
            out int symbolOffset,
            out ISECT resultSeg,
            out int resultOffsetIndex)
        {
            //Section number is 1-based, and we want the previous one to that
            //so do -2
            for (var i = sectionNumber - 2; i >= 0; i--)
            {
                var offsets = offsetTable[i];

                if (offsets.Length > 0)
                {
                    //Use the last symbol of the previous segment
                    var last = offsets[offsets.Length - 1];
                    symbolOffset = last.symbolOffset;
                    resultOffsetIndex = offsets.Length - 1;
                    resultSeg = i + 1;
                    return true;
                }
            }

            symbolOffset = default;
            resultSeg = default;
            resultOffsetIndex = default;
            return false;
        }

        #endregion

        internal static bool GetNearestSymbolInternal(
            IAddrHashInternal32 addrHash32,
            ISECT sectionNumber,
            int relativeOffset,
            out int symbolOffset)
        {
            addrHash32.BinarySearchAddressMap(relativeOffset, sectionNumber, out var resultSeg, out var resultOffsetIndex);

            if (sectionNumber == resultSeg)
            {
                //Handle Identical Code Folding (ICF). Based on the implementation in AddressMapSymTypeList.GetNearestSymbol

                var currentOffset = addrHash32[sectionNumber, resultOffsetIndex].sectionRelativeOffset;

                while (resultOffsetIndex > 0)
                {
                    var previousItemIndex = resultOffsetIndex - 1;

                    var previousOffset = addrHash32[sectionNumber, previousItemIndex].sectionRelativeOffset;

                    //Any symbols where ICF is present should be all in a row, so if the previous symbol has a different offset,
                    //we're done
                    if (previousOffset != currentOffset)
                        break;

                    resultOffsetIndex = previousOffset;
                }

                symbolOffset = addrHash32[sectionNumber, resultOffsetIndex].symbolOffset;
                return true;
            }
            else
            {
                /* If the symbol we matched against was the last symbol in the given section before the section we're actually after,
                 * we need to advance to the first symbol in the next section
                 * 
                 * Unlike with AddressMapSymTypeList, we can just skip to the target section
                 */

                if (sectionNumber > addrHash32.cSeg || addrHash32.GetOffsetCount(sectionNumber) == 0)
                {
                    symbolOffset = default;
                    return false;
                }

                var first = addrHash32[sectionNumber, 0];
                symbolOffset = first.symbolOffset;
                return true;
            }
        }
    }
}
