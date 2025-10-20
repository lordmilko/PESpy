using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET9_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using System.Text;

namespace PESpy
{
    public static unsafe class StringHelpers
    {
        /* After having performed extensive testing on null terminated string comparisons
         * in .NET, I can conclude the following items
         *
         * - The fastest way to manipulate strings is using hardware intrinsics
         *
         * - You can beat native implementations of strcmp, strncmp, stricmp, strnicmp and strlen, even with the added
         *   cost of having to calculate the length of the string up front, even when using SuppressGCTransition
         *
         * - Using specialized hardware intrinsics of calculating the length of the string is also way faster than performing
         *   a naive Span.IndexOf
         *
         *   On my machine, I can compare the string "SetupThread" 1,000,000 times using SequenceEquals and its known length in 0.4ms.
         *   strncmp with SuppressGCTransition takes 1.7695ms, and yet incredibly you can use AVX to calculate the length and then do SequenceEquals
         *   in 1.7338ms - beating out the native implementation!
         *
         * - You can get the length of a null terminated string fastest with AVX, which is
         *   way faster than doing Span.IndexOf and is also faster than trying to re-implement
         *   the logic of strlen written in handrolled assembly. It's marginally faster to unroll
         *   your loops when doing comparisons with AVX, although many strings will be too small
         *   to even meet the threshold for the second comparison
         *
         * - You do not need to worry about aligning the values you pass into AVX. In fact it's significantly
         *   slower to manually try and align your strings - you might just end up checking the length
         *   of the whole string manually
         *
         * - With smaller strings, Vector128 is marginally faster than Vector256, but once you get
         *   to larger strings, Vector256 clearly becomes better
         */

        //Note: dotnet/runtime has a method IndexOfNullByte which is a big fancy implementation of locating a null terminator
        //which can even handle reading out of bounds without crashing, but my implementation is 4x faster than it
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStringLength(byte* str)
        {
            if (str == default)
                return 0;

#if NET9_0_OR_GREATER
            var p = str;

            if (Avx2.IsSupported)
            {
                var zero = Vector256<byte>.Zero;

                while (true)
                {
                    //Vector256<byte>.Count is 32. We unroll the loop and process two chunks per iteration
                    var chunk0 = Avx.LoadVector256(p);
                    var mask0 = Avx2.MoveMask(Avx2.CompareEqual(chunk0, zero));

                    if (mask0 != 0)
                        return (int) (p - str) + BitOperations.TrailingZeroCount(mask0);

                    var chunk1 = Avx.LoadVector256(p + 32);
                    var mask1 = Avx2.MoveMask(Avx2.CompareEqual(chunk1, zero));

                    if (mask1 != 0)
                        return (int) (p - str) + 32 + BitOperations.TrailingZeroCount(mask1);

                    p += 64; //2*Vector256<byte>.Count
                }
            }
            else if (Sse2.IsSupported)
            {
                var zero = Vector128<byte>.Zero;

                while (true)
                {
                    //Vector128<byte>.Count is 16. We unroll the loop and process two chunks per iteration
                    var chunk0 = Sse2.LoadVector128(p);
                    var mask0 = Sse2.MoveMask(Sse2.CompareEqual(chunk0, zero));

                    if (mask0 != 0)
                        return (int) (p - str) + BitOperations.TrailingZeroCount(mask0);

                    var chunk1 = Sse2.LoadVector128(p + 16);
                    var mask1 = Sse2.MoveMask(Sse2.CompareEqual(chunk1, zero));

                    if (mask1 != 0)
                        return (int) (p - str) + 16 + BitOperations.TrailingZeroCount(mask1);

                    p += 32; //2*Vector256<byte>.Count
                }
            }
#endif
            //Where possible, we want to use a specialized hardware intrinsics for calculating
            //the length of a null terminated string. However, if hardware intrinsics aren't available
            //(or our target framework does not support the use of hardware intrinsics) fall back to whatever
            //Span.IndexOf is capable of (which should be faster than a naive while loop)
            return new Span<byte>(str, int.MaxValue).IndexOf((byte) 0);
        }

        #region Exact (ANSI/UTF-8)

        //I believe I saw it's faster to get the length inline here, so we have a separate overload for where you already have a span
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(byte* str1, byte* str2) =>
            new Span<byte>(str1, GetStringLength(str1)).SequenceEqual(new Span<byte>(str2, GetStringLength(str2)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(byte* str1, ReadOnlySpan<byte> str2) =>
            new Span<byte>(str1, GetStringLength(str1)).SequenceEqual(str2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(byte* str1, byte* str2) =>
            new Span<byte>(str1, GetStringLength(str1)).IndexOf(new Span<byte>(str2, GetStringLength(str2)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(byte* str1, byte* str2) => IndexOf(str1, str2) != -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareTo(byte* str1, byte* str2) =>
            new Span<byte>(str1, GetStringLength(str1)).SequenceCompareTo(new Span<byte>(str2, GetStringLength(str2)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(byte* str1, byte* str2) =>
            new Span<byte>(str1, GetStringLength(str1)).StartsWith(new Span<byte>(str2, GetStringLength(str2)));

        //This is faster than passing in a Span
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(byte* str1, byte* str2) =>
            new Span<byte>(str1, GetStringLength(str1)).EndsWith(new Span<byte>(str2, GetStringLength(str2)));

        #endregion
        #region Ignore Case (ANSI/UTF-8)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsIgnoreCase(byte* str1, byte* str2) =>
            new ReadOnlySpan<byte>(str1, GetStringLength(str1)).EqualsOrdinalIgnoreCaseUtf8(new ReadOnlySpan<byte>(str2, GetStringLength(str2)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsIgnoreCase(byte* str1, ReadOnlySpan<byte> str2) =>
            new ReadOnlySpan<byte>(str1, GetStringLength(str1)).EqualsOrdinalIgnoreCaseUtf8(str2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfIgnoreCase(byte* str1, ReadOnlySpan<byte> str2) =>
            System.Globalization.Ordinal.IndexOfOrdinalIgnoreCase(new ReadOnlySpan<byte>(str1, GetStringLength(str1)), str2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsIgnoreCase(byte* str1, ReadOnlySpan<byte> str2) => IndexOfIgnoreCase(str1, str2) != -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithIgnoreCase(byte* str1, byte* str2)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithIgnoreCase(byte* str1, byte* str2)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Exact (ANSI/UTF-8 -> String)

        //Compare an ANSI/UTF-8 pointer against a UTF-16 String, with the assumption that both strings only
        //contain characters in the ASCII code range

        public static bool Equals(byte* str1, string str2)
        {
            /* NOTE: this method is structured in a very specific way to try and have the JIT emit something
             * with the optimum performance. Changes as trivial as hoisting variables to outer scopes can easily
             * have a detrimental effect on code gen. Do not make any changes without benchmarking this! */

            //It's faster to construct the spans in this method than pass them in
            var str1Length = GetStringLength(str1);

            if (str1Length != str2.Length)
                return false;

            ref var refStr1 = ref Unsafe.AsRef<byte>(str1);
            ref var refStr2 = ref MemoryMarshal.GetReference(str2.AsSpan());

#if NET9_0_OR_GREATER
            var i = 0;

            if (Sse2.IsSupported)
            {
                //We compare using Vector128 which operates on 16 byte payloads. As such, get the maximum
                //number of bytes we could possibly process that is a multiple of 16
                var last = str1Length - Vector128<byte>.Count;

                //If the input is 15 bytes, limit will be -1 and we won't enter. If the input is 17, the limit will be 1
                //so we'll process the first 16 bytes and then fallback to processing the remaining byte another way
                if (last >= 0)
                {
                    //Do NOT hoist this outside the if to share with the tail handling code; this seems to result in
                    //slower codegen
                    ref var charsAsShort = ref Unsafe.As<char, short>(ref refStr2);

                    for (; i <= last; i += 16)
                    {
                        //Do NOT hoist any of these variables outside the if to share with the tail handling code; this seems to result in
                        //slower codegen

                        var vecStr1 = Vector128.LoadUnsafe(ref Unsafe.Add(ref refStr1, i));

                        // Load 16 chars (as shorts) in two halves
                        Vector128<short> lo = Vector128.LoadUnsafe(ref Unsafe.Add(ref charsAsShort, i));
                        Vector128<short> hi = Vector128.LoadUnsafe(ref Unsafe.Add(ref charsAsShort, i + 8));

                        // Narrow to 16 bytes (saturates, but ASCII chars ≤ 0x7F)
                        Vector128<byte> vecStr2 = Sse2.PackUnsignedSaturate(lo, hi);

                        // Compare
                        Vector128<byte> cmp = Sse2.CompareEqual(vecStr1, vecStr2);
                        int mask = Sse2.MoveMask(cmp);

                        if (mask != 0xFFFF)
                            return false;
                    }
                }

                /* For the remaining bytes, we basically just need to do the same thing as the above, except
                 * we mask out the result to only take into consideration the remaining bytes that we're yet to consider.
                 *
                 * I'm not exactly 100% sure how safe this is; technically speaking, we're reading beyond the length
                 * of the string into invalid memory
                 *
                 * These variables all have the same names, but they can't literally share the same variables as above;
                 * doing so results in slower codegen (perhaps due to having to preserve the variable's lifetimes?)
                 */

                {
                    var remaining = str1Length - i;

                    //This may seem unnecessary (we could just do the add inline below), but we seem to be slower
                    //without it
                    ref short tailCharsAsShort = ref Unsafe.As<char, short>(ref Unsafe.Add(ref refStr2, i));
                    ref byte tailBytes = ref Unsafe.Add(ref refStr1, i);

                    Vector128<short> lo = Vector128.LoadUnsafe(ref tailCharsAsShort);
                    Vector128<short> hi = Vector128.LoadUnsafe(ref Unsafe.Add(ref tailCharsAsShort, 8));

                    Vector128<byte> vecStr2 = Sse2.PackUnsignedSaturate(lo, hi);
                    var vecStr1 = Vector128.LoadUnsafe(ref tailBytes);

                    Vector128<byte> cmp = Sse2.CompareEqual(vecStr1, vecStr2);
                    int mask = Sse2.MoveMask(cmp);

                    //Clear out any bits we we're trying to consider
                    var bitMask = (1 << remaining) - 1;

                    mask &= bitMask;

                    return mask == bitMask;
                }
            }
#endif

            //Fallback to an unrolled SequenceEqual implementation
            return SequenceEqual(ref refStr1, ref refStr2, str1Length);
        }

        //From dotnet/runtime
        private static bool SequenceEqual(ref byte first, ref char second, int length)
        {
            nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations
            byte lookUp0;
            char lookUp1;
            while (length >= 8)
            {
                length -= 8;

                lookUp0 = Unsafe.Add(ref first, index);
                lookUp1 = Unsafe.Add(ref second, index);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 1);
                lookUp1 = Unsafe.Add(ref second, index + 1);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 2);
                lookUp1 = Unsafe.Add(ref second, index + 2);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 3);
                lookUp1 = Unsafe.Add(ref second, index + 3);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 4);
                lookUp1 = Unsafe.Add(ref second, index + 4);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 5);
                lookUp1 = Unsafe.Add(ref second, index + 5);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 6);
                lookUp1 = Unsafe.Add(ref second, index + 6);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 7);
                lookUp1 = Unsafe.Add(ref second, index + 7);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;

                index += 8;
            }

            if (length >= 4)
            {
                length -= 4;

                lookUp0 = Unsafe.Add(ref first, index);
                lookUp1 = Unsafe.Add(ref second, index);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 1);
                lookUp1 = Unsafe.Add(ref second, index + 1);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 2);
                lookUp1 = Unsafe.Add(ref second, index + 2);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 3);
                lookUp1 = Unsafe.Add(ref second, index + 3);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;

                index += 4;
            }

            while (length > 0)
            {
                lookUp0 = Unsafe.Add(ref first, index);
                lookUp1 = Unsafe.Add(ref second, index);
                if (lookUp0 != (byte) lookUp1)
                    goto NotEqual;
                index += 1;
                length--;
            }

        Equal:
            return true;

        NotEqual: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return false;
        }

        #endregion
    }
}
