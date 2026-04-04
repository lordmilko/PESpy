// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class MemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsOrdinalIgnoreCaseUtf8(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
        {
            // For UTF-8 it is possible for two spans of different byte length
            // to compare as equal under an OrdinalIgnoreCase comparison.

            if ((span.Length | value.Length) == 0)  // span.Length == value.Length == 0
            {
                return true;
            }

            return Ordinal.EqualsIgnoreCaseUtf8(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }

#if NET9_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool StartsWithOrdinalIgnoreCaseUtf8(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> value)
        {
            // For UTF-8 it is possible for two spans of different byte length
            // to compare as equal under an OrdinalIgnoreCase comparison.

            if ((span.Length | value.Length) == 0)  // span.Length == value.Length == 0
            {
                return true;
            }

            return Ordinal.StartsWithIgnoreCaseUtf8(ref MemoryMarshal.GetReference(span), span.Length, ref MemoryMarshal.GetReference(value), value.Length);
        }
#endif
    }
}
