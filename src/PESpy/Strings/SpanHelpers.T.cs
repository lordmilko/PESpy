// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
#if NET9_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace System
{
    internal static partial class SpanHelpers // .T
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfChar(ref byte searchSpace, byte value, int length)
            => IndexOfValueType(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfValueType(ref byte searchSpace, byte value, int length)
        {
            return IndexOfValueType<DontNegate<byte>>(
                ref searchSpace,
                value,
                length
#if !NET9_0_OR_GREATER
                , DontNegate<byte>.Instance
#endif
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfValueType<TNegator>(
            ref byte searchSpace,
            byte value,
            int length
#if !NET9_0_OR_GREATER
            , TNegator negator
#endif
            )
            where TNegator : struct, INegator<byte>
        {
            //We don't need to pack because we're not UTF-16!
            return NonPackedIndexOfValueType<TNegator>(
                ref searchSpace,
                value,
                length
#if !NET9_0_OR_GREATER
                , negator
#endif
                );
        }

        internal static int NonPackedIndexOfValueType<TNegator>(
            ref byte searchSpace,
            byte value,
            int length
#if !NET9_0_OR_GREATER
            , TNegator negator
#endif
            )
            where TNegator : struct, INegator<byte>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

#if NET9_0_OR_GREATER
            if (!Vector128.IsHardwareAccelerated || length < Vector128<byte>.Count)
#endif
            {
                nuint offset = 0;

                while (length >= 8)
                {
                    length -= 8;

#if !NET9_0_OR_GREATER
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) goto Found1;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) goto Found2;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) goto Found3;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 4) == value)) goto Found4;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 5) == value)) goto Found5;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 6) == value)) goto Found6;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 7) == value)) goto Found7;
#else
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) goto Found1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) goto Found2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) goto Found3;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 4) == value)) goto Found4;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 5) == value)) goto Found5;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 6) == value)) goto Found6;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 7) == value)) goto Found7;
#endif

                    offset += 8;
                }

                if (length >= 4)
                {
                    length -= 4;

#if !NET9_0_OR_GREATER
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) goto Found1;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) goto Found2;
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) goto Found3;
#else
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) goto Found1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) goto Found2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) goto Found3;
#endif

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

#if !NET9_0_OR_GREATER
                    if (negator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
#else
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
#endif

                    offset += 1;
                }
                return -1;
            Found7:
                return (int)(offset + 7);
            Found6:
                return (int)(offset + 6);
            Found5:
                return (int)(offset + 5);
            Found4:
                return (int)(offset + 4);
            Found3:
                return (int)(offset + 3);
            Found2:
                return (int)(offset + 2);
            Found1:
                return (int)(offset + 1);
            Found:
                return (int)(offset);
            }
#if NET9_0_OR_GREATER
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<byte>.Count)
            {
                Vector512<byte> current, values = Vector512.Create(value);
                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);

                    if (TNegator.HasMatch(values, current))
                    {
                        return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, TNegator.GetMatchMask(values, current));
                    }

                    currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<byte>.Count);
                }
                while (IsAddressLessThanOrEqualTo(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<byte>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);

                    if (TNegator.HasMatch(values, current))
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, TNegator.GetMatchMask(values, current));
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
            {
                Vector256<byte> equals, values = Vector256.Create(value);
                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values, Vector256.LoadUnsafe(ref currentSearchSpace)));
                    if (equals == Vector256<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (IsAddressLessThanOrEqualTo(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<byte>.Count != 0)
                {
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values, Vector256.LoadUnsafe(ref oneVectorAwayFromEnd)));
                    if (equals != Vector256<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<byte> equals, values = Vector128.Create(value);
                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace)));
                    if (equals == Vector128<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (IsAddressLessThanOrEqualTo(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<byte>.Count != 0)
                {
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values, Vector128.LoadUnsafe(ref oneVectorAwayFromEnd)));
                    if (equals != Vector128<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyChar(ref byte searchSpace, byte value0, byte value1, int length)
            => IndexOfAnyValueType(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType(ref byte searchSpace, byte value0, byte value1, int length)
        {
            return IndexOfAnyValueType<DontNegate<byte>>(
                ref searchSpace,
                value0,
                value1,
                length
#if !NET9_0_OR_GREATER
                , DontNegate<byte>.Instance
#endif
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfAnyValueType<TNegator>(
            ref byte searchSpace,
            byte value0,
            byte value1,
            int length
#if !NET9_0_OR_GREATER
            , TNegator negator
#endif
            )
            where TNegator : struct, INegator<byte>
        {
            //We don't need to pack because we're not UTF-16!
            return NonPackedIndexOfAnyValueType<TNegator>(
                ref searchSpace,
                value0,
                value1,
                length
#if !NET9_0_OR_GREATER
                , negator
#endif
                );
        }

        // having INumber<T> constraint here allows to use == operator and get better perf compared to .Equals
        internal static int NonPackedIndexOfAnyValueType<TNegator>(
            ref byte searchSpace,
            byte value0,
            byte value1,
            int length
#if !NET9_0_OR_GREATER
            , TNegator negator
#endif
            )
            where TNegator : struct, INegator<byte>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

#if NET9_0_OR_GREATER
            if (!Vector128.IsHardwareAccelerated || length < Vector128<byte>.Count)
#endif
            {
                nuint offset = 0;
                byte lookUp;

                while (length >= 8)
                {
                    length -= 8;

                    ref byte current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;

#if !NET9_0_OR_GREATER
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found3;
                    lookUp = Unsafe.Add(ref current, 4);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found4;
                    lookUp = Unsafe.Add(ref current, 5);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found5;
                    lookUp = Unsafe.Add(ref current, 6);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found6;
                    lookUp = Unsafe.Add(ref current, 7);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found7;
#else
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found3;
                    lookUp = Unsafe.Add(ref current, 4);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found4;
                    lookUp = Unsafe.Add(ref current, 5);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found5;
                    lookUp = Unsafe.Add(ref current, 6);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found6;
                    lookUp = Unsafe.Add(ref current, 7);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found7;
#endif

                    offset += 8;
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref byte current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;

#if !NET9_0_OR_GREATER
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found3;
#else
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found3;
#endif

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);

#if !NET9_0_OR_GREATER
                    if (negator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
#else
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
#endif

                    offset += 1;
                }
                return -1;
            Found7:
                return (int)(offset + 7);
            Found6:
                return (int)(offset + 6);
            Found5:
                return (int)(offset + 5);
            Found4:
                return (int)(offset + 4);
            Found3:
                return (int)(offset + 3);
            Found2:
                return (int)(offset + 2);
            Found1:
                return (int)(offset + 1);
            Found:
                return (int)(offset);
            }
#if NET9_0_OR_GREATER
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<byte>.Count)
            {
                Vector512<byte> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1);
                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current));
                    if (equals == Vector512<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (IsAddressLessThanOrEqualTo(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<byte>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current));
                    if (equals != Vector512<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<byte>.Count)
            {
                Vector256<byte> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1);
                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current));
                    if (equals == Vector256<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (IsAddressLessThanOrEqualTo(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<byte>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current));
                    if (equals != Vector256<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<byte> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1);
                ref byte currentSearchSpace = ref searchSpace;
                ref byte oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<byte>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current));
                    if (equals == Vector128<byte>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<byte>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (IsAddressLessThanOrEqualTo(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<byte>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current));
                    if (equals != Vector128<byte>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
#endif
        }

#if NET9_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex(ref byte searchSpace, ref byte current, Vector128<byte> equals)
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex(ref byte searchSpace, ref byte current, Vector256<byte> equals)
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex(ref byte searchSpace, ref byte current, Vector512<byte> equals)
        {
            ulong notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current));
        }
#endif

        internal interface INegator<T> where T : struct
        {
#if !NET9_0_OR_GREATER
            bool NegateIfNeeded(bool equals);
#else
            static abstract bool NegateIfNeeded(bool equals);
#endif

#if NET9_0_OR_GREATER
            static abstract Vector128<T> NegateIfNeeded(Vector128<T> equals);
            static abstract Vector256<T> NegateIfNeeded(Vector256<T> equals);
            static abstract Vector512<T> NegateIfNeeded(Vector512<T> equals);

            // The generic vector APIs assume use for IndexOf where `DontNegate` is
            // for `IndexOfAny` and `Negate` is for `IndexOfAnyExcept`

            static abstract bool HasMatch(Vector512<T> left, Vector512<T> right);

            static abstract Vector512<T> GetMatchMask(Vector512<T> left, Vector512<T> right);
#endif
        }

        internal readonly struct DontNegate<T> : INegator<T>
            where T : struct
        {
#if !NET9_0_OR_GREATER
            public static readonly DontNegate<T> Instance;
            public bool NegateIfNeeded(bool equals) => equals;
#else
            public static bool NegateIfNeeded(bool equals) => equals;
#endif

#if NET9_0_OR_GREATER
            public static Vector128<T> NegateIfNeeded(Vector128<T> equals) => equals;
            public static Vector256<T> NegateIfNeeded(Vector256<T> equals) => equals;
            public static Vector512<T> NegateIfNeeded(Vector512<T> equals) => equals;

            // The generic vector APIs assume use for `IndexOfAny` where we
            // want "HasMatch" to mean any of the two elements match.

            public static bool HasMatch(Vector512<T> left, Vector512<T> right)
            {
                return Vector512.EqualsAny(left, right);
            }

            public static Vector512<T> GetMatchMask(Vector512<T> left, Vector512<T> right)
            {
                return Vector512.Equals(left, right);
            }
#endif
        }

        internal readonly struct Negate<T> : INegator<T>
            where T : struct
        {
#if !NET9_0_OR_GREATER
            public static readonly DontNegate<T> Instance;
            public bool NegateIfNeeded(bool equals) => !equals;
#else
            public static bool NegateIfNeeded(bool equals) => !equals;
#endif

#if NET9_0_OR_GREATER
            public static Vector128<T> NegateIfNeeded(Vector128<T> equals) => ~equals;
            public static Vector256<T> NegateIfNeeded(Vector256<T> equals) => ~equals;
            public static Vector512<T> NegateIfNeeded(Vector512<T> equals) => ~equals;

            // The generic vector APIs assume use for `IndexOfAnyExcept` where we
            // want "HasMatch" to mean any of the two elements don't match

            public static bool HasMatch(Vector512<T> left, Vector512<T> right)
            {
                return !Vector512.EqualsAll(left, right);
            }

            public static Vector512<T> GetMatchMask(Vector512<T> left, Vector512<T> right)
            {
                return ~Vector512.Equals(left, right);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAddressLessThanOrEqualTo<T>(ref T left, ref T right) => Unsafe.IsAddressGreaterThan(ref left, ref right);
    }
}
