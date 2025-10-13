// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NETSTANDARD
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using System.Text.Unicode;

namespace System.Globalization
{
    internal static partial class Ordinal
    {
        public static bool IsAscii(char c) => (uint)c <= '\x007f';

        internal static int IndexOfOrdinalIgnoreCase(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
            if (value.Length == 0)
            {
                return 0;
            }

            if (value.Length > source.Length)
            {
                // A non-linguistic search compares chars directly against one another, so large
                // target strings can never be found inside small search spaces. This check also
                // handles empty 'source' spans.
                return -1;
            }

            // If value doesn't start with ASCII, fall back to a non-vectorized non-ASCII friendly version.
            ref byte valueRef = ref MemoryMarshal.GetReference(value);
            byte valueChar = valueRef;
            if (!IsAscii((char) valueChar))
            {
                //return OrdinalCasing.IndexOf(source, value);
                throw new NotImplementedException();
            }

            // Hoist some expressions from the loop
            int valueTailLength = value.Length - 1;
            int searchSpaceMinusValueTailLength = source.Length - valueTailLength;
            ref byte searchSpace = ref MemoryMarshal.GetReference(source);
            byte valueCharU = default;
            byte valueCharL = default;
            nint offset = 0;
            bool isLetter = false;

            // If the input is long enough and the value ends with ASCII and is at least two characters,
            // we can take a special vectorized path that compares both the beginning and the end at the same time.
#if !NETSTANDARD
            if (Vector128.IsHardwareAccelerated && valueTailLength != 0 && searchSpaceMinusValueTailLength >= Vector128<byte>.Count)
            {
                valueCharU = Unsafe.Add(ref valueRef, valueTailLength);
                if (char.IsAscii((char) valueCharU))
                {
                    goto SearchTwoChars;
                }
            }
#endif

            // We're searching for the first character and it's known to be ASCII. If it's not a letter,
            // then IgnoreCase doesn't impact what it matches and we just need to do a normal search
            // for that single character. If it is a letter, then we need to search for both its upper
            // and lower-case variants.
            if (IsAsciiLetter((char) valueChar))
            {
                valueCharU = (byte) (valueChar & ~0x20);
                valueCharL = (byte) (valueChar | 0x20);
                isLetter = true;
            }

            do
            {
                // Do a quick search for the first element of "value".
                int relativeIndex = isLetter ?
                        SpanHelpers.IndexOfAnyChar(ref Unsafe.Add(ref searchSpace, offset), valueCharU, valueCharL, searchSpaceMinusValueTailLength) :
                    SpanHelpers.IndexOfChar(ref Unsafe.Add(ref searchSpace, offset), valueChar, searchSpaceMinusValueTailLength);
                if (relativeIndex < 0)
                {
                    break;
                }

                searchSpaceMinusValueTailLength -= relativeIndex;
                if (searchSpaceMinusValueTailLength <= 0)
                {
                    break;
                }
                offset += relativeIndex;

                // Found the first element of "value". See if the tail matches.
                if (valueTailLength == 0 || // for single-char values we already matched first chars
                    EqualsIgnoreCaseUtf8(
                        ref Unsafe.Add(ref searchSpace, (nuint)(offset + 1)), valueTailLength,
                        ref Unsafe.Add(ref valueRef, 1), valueTailLength))
                {
                    return (int)offset;  // The tail matched. Return a successful find.
                }

                searchSpaceMinusValueTailLength--;
                offset++;
            }
            while (searchSpaceMinusValueTailLength > 0);

            return -1;

#if !NETSTANDARD

        // Based on SpanHelpers.IndexOf(ref char, int, ref char, int), which was in turn based on
        // http://0x80.pl/articles/simd-strfind.html#algorithm-1-generic-simd. This version has additional
        // modifications to support case-insensitive searches.
        SearchTwoChars:
            // Both the first character in value (valueChar) and the last character in value (valueCharU) are ASCII. Get their lowercase variants.
            valueChar = (byte)(valueChar | 0x20);
            valueCharU = (byte)(valueCharU | 0x20);

            // The search is more efficient if the two characters being searched for are different. As long as they are equal, walk backwards
            // from the last character in the search value until we find a character that's different. Since we're dealing with IgnoreCase,
            // we compare the lowercase variants, as that's what we'll be comparing against in the main loop.
            nint ch1ch2Distance = valueTailLength;
            while (valueCharU == valueChar && ch1ch2Distance > 1)
            {
                byte tmp = Unsafe.Add(ref valueRef, ch1ch2Distance - 1);
                if (!char.IsAscii((char) tmp))
                {
                    break;
                }
                --ch1ch2Distance;
                valueCharU = (byte)(tmp | 0x20);
            }

            // Use Vector256 if the input is long enough.
            if (Vector256.IsHardwareAccelerated && searchSpaceMinusValueTailLength - Vector256<byte>.Count >= 0)
            {
                // Create a vector for each of the lowercase ASCII characters we're searching for.
                Vector256<byte> ch1 = Vector256.Create(valueChar);
                Vector256<byte> ch2 = Vector256.Create(valueCharU);

                nint searchSpaceMinusValueTailLengthAndVector = searchSpaceMinusValueTailLength - (nint)Vector256<byte>.Count;
                do
                {
                    // Make sure we don't go out of bounds.
                    Debug.Assert(offset + ch1ch2Distance + Vector256<byte>.Count <= source.Length);

                    // Load a vector from the current search space offset and another from the offset plus the distance between the two characters.
                    // For each, | with 0x20 so that letters are lowercased, then & those together to get a mask. If the mask is all zeros, there
                    // was no match.  If it wasn't, we have to do more work to check for a match.
                    Vector256<byte> cmpCh2 = Vector256.Equals<byte>(ch2, Vector256.BitwiseOr(Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)), Vector256.Create((byte)0x20)));
                    Vector256<byte> cmpCh1 = Vector256.Equals<byte>(ch1, Vector256.BitwiseOr(Vector256.LoadUnsafe(ref searchSpace, (nuint)offset), Vector256.Create((byte)0x20)));
                    Vector256<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();
                    if (cmpAnd != Vector256<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    // No match. Advance to the next vector.
                    offset += Vector256<byte>.Count;

                    // If we've reached the end of the search space, bail.
                    if (offset == searchSpaceMinusValueTailLength)
                    {
                        return -1;
                    }

                    // If we're within a vector's length of the end of the search space, adjust the offset
                    // to point to the last vector so that our next iteration will process it.
                    if (offset > searchSpaceMinusValueTailLengthAndVector)
                    {
                        offset = searchSpaceMinusValueTailLengthAndVector;
                    }

                    continue;

                CandidateFound:
                    // Possible matches at the current location. Extract the bits for each element.
                    // For each set bits, we'll check if it's a match at that location.
                    uint mask = cmpAnd.ExtractMostSignificantBits();
                    do
                    {
                        // Do a full IgnoreCase equality comparison. SpanHelpers.IndexOf skips comparing the two characters in some cases,
                        // but we don't actually know that the two characters are equal, since we compared with | 0x20. So we just compare
                        // the full string always.
                        nint charPos = (nint)uint.TrailingZeroCount(mask);
                        if (EqualsIgnoreCaseUtf8(ref Unsafe.Add(ref searchSpace, offset + charPos), value.Length, ref valueRef, value.Length))
                        {
                            // Match! Return the index.
                            return (int)(offset + charPos);
                        }

                        // Clear the two lowest set bits in the mask. If there are no more set bits, we're done.
                        // If any remain, we loop around to do the next comparison.
                        mask = ResetLowestSetBit(ResetLowestSetBit(mask));
                    } while (mask != 0);
                    goto LoopFooter;

                } while (true);
            }
            else // 128bit vector path (SSE2 or AdvSimd)
            {
                // Create a vector for each of the lowercase ASCII characters we're searching for.
                Vector128<byte> ch1 = Vector128.Create(valueChar);
                Vector128<byte> ch2 = Vector128.Create(valueCharU);

                nint searchSpaceMinusValueTailLengthAndVector = searchSpaceMinusValueTailLength - (nint)Vector128<byte>.Count;
                do
                {
                    // Make sure we don't go out of bounds.
                    Debug.Assert(offset + ch1ch2Distance + Vector128<byte>.Count <= source.Length);

                    // Load a vector from the current search space offset and another from the offset plus the distance between the two characters.
                    // For each, | with 0x20 so that letters are lowercased, then & those together to get a mask. If the mask is all zeros, there
                    // was no match.  If it wasn't, we have to do more work to check for a match.
                    Vector128<byte> cmpCh2 = Vector128.Equals<byte>(ch2, Vector128.BitwiseOr(Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset + ch1ch2Distance)), Vector128.Create((byte)0x20)));
                    Vector128<byte> cmpCh1 = Vector128.Equals<byte>(ch1, Vector128.BitwiseOr(Vector128.LoadUnsafe(ref searchSpace, (nuint)offset), Vector128.Create((byte)0x20)));
                    Vector128<byte> cmpAnd = (cmpCh1 & cmpCh2).AsByte();
                    if (cmpAnd != Vector128<byte>.Zero)
                    {
                        goto CandidateFound;
                    }

                LoopFooter:
                    // No match. Advance to the next vector.
                    offset += Vector128<byte>.Count;

                    // If we've reached the end of the search space, bail.
                    if (offset == searchSpaceMinusValueTailLength)
                    {
                        return -1;
                    }

                    // If we're within a vector's length of the end of the search space, adjust the offset
                    // to point to the last vector so that our next iteration will process it.
                    if (offset > searchSpaceMinusValueTailLengthAndVector)
                    {
                        offset = searchSpaceMinusValueTailLengthAndVector;
                    }

                    continue;

                CandidateFound:
                    // Possible matches at the current location. Extract the bits for each element.
                    // For each set bits, we'll check if it's a match at that location.
                    uint mask = cmpAnd.ExtractMostSignificantBits();
                    do
                    {
                        // Do a full IgnoreCase equality comparison. SpanHelpers.IndexOf skips comparing the two characters in some cases,
                        // but we don't actually know that the two characters are equal, since we compared with | 0x20. So we just compare
                        // the full string always.
                        nint charPos = (nint)uint.TrailingZeroCount(mask);
                        if (EqualsIgnoreCaseUtf8(ref Unsafe.Add(ref searchSpace, offset + charPos), value.Length, ref valueRef, value.Length))
                        {
                            // Match! Return the index.
                            return (int)(offset + charPos);
                        }

                        // Clear the two lowest set bits in the mask. If there are no more set bits, we're done.
                        // If any remain, we loop around to do the next comparison.
                        mask = ResetLowestSetBit(ResetLowestSetBit(mask));
                    } while (mask != 0);
                    goto LoopFooter;

                } while (true);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ResetLowestSetBit(uint value)
        {
            // It's lowered to BLSR on x86
            return value & (value - 1);
        }
    }
}
