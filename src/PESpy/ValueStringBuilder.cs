// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using PESpy;

#nullable enable

namespace PESpy
{
    /// <summary>
    /// Represents a non-allocating string builder capable of being backed
    /// by either stack memory or a rented array.
    /// </summary>
    internal ref partial struct ValueStringBuilder
    {
        private string DebuggerDisplay => ToString();

        private char[]? _arrayToReturnToPool;
        private Span<char> _chars;
        private int _pos;

        public ValueStringBuilder(Span<char> initialBuffer)
        {
            _arrayToReturnToPool = null;
            _chars = initialBuffer;
            _pos = 0;
        }

        public ValueStringBuilder(int initialCapacity)
        {
            _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            _chars = _arrayToReturnToPool;
            _pos = 0;
        }

        public int Length
        {
            get => _pos;
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(value <= _chars.Length);
                _pos = value;
            }
        }

        public int Capacity => _chars.Length;

        public void EnsureCapacity(int capacity)
        {
            // This is not expected to be called this with negative capacity
            Debug.Assert(capacity >= 0);

            // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
            if ((uint) capacity > (uint) _chars.Length)
                Grow(capacity - _pos);
        }

        /// <summary>
        /// Get a pinnable reference to the builder.
        /// Does not ensure there is a null char after <see cref="Length"/>
        /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
        /// the explicit method call, and write eg "fixed (char* c = builder)"
        /// </summary>
        public ref char GetPinnableReference()
        {
            return ref MemoryMarshal.GetReference(_chars);
        }

        /// <summary>
        /// Get a pinnable reference to the builder.
        /// </summary>
        /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
        public ref char GetPinnableReference(bool terminate)
        {
            if (terminate)
            {
                EnsureCapacity(Length + 1);
                _chars[Length] = '\0';
            }
            return ref MemoryMarshal.GetReference(_chars);
        }

        public ref char this[int index]
        {
            get
            {
                Debug.Assert(index < _pos);
                return ref _chars[index];
            }
        }

        public override string ToString()
        {
            string s = _chars.Slice(0, _pos).ToString();
            return s;
        }

        /// <summary>Returns the underlying storage of the builder.</summary>
        public Span<char> RawChars => _chars;

        /// <summary>
        /// Returns a span around the contents of the builder.
        /// </summary>
        /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
        public ReadOnlySpan<char> AsSpan(bool terminate)
        {
            if (terminate)
            {
                EnsureCapacity(Length + 1);
                _chars[Length] = '\0';
            }
            return _chars.Slice(0, _pos);
        }

        public ReadOnlySpan<char> AsSpan() => _chars.Slice(0, _pos);
        public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);
        public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

        public bool TryCopyTo(Span<char> destination, out int charsWritten)
        {
            if (_chars.Slice(0, _pos).TryCopyTo(destination))
            {
                charsWritten = _pos;
                Dispose();
                return true;
            }
            else
            {
                charsWritten = 0;
                Dispose();
                return false;
            }
        }

        public void Insert(int index, char value, int count)
        {
            if (_pos > _chars.Length - count)
            {
                Grow(count);
            }

            int remaining = _pos - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            _chars.Slice(index, count).Fill(value);
            _pos += count;
        }

        public void Insert(int index, string? s)
        {
            if (s == null)
            {
                return;
            }

            int count = s.Length;

            if (_pos > (_chars.Length - count))
            {
                Grow(count);
            }

            int remaining = _pos - index;
            _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
            s.AsSpan().CopyTo(_chars.Slice(index));
            _pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            int pos = _pos;
            Span<char> chars = _chars;
            if ((uint) pos < (uint) chars.Length)
            {
                chars[pos] = c;
                _pos = pos + 1;
            }
            else
            {
                GrowAndAppend(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(long value)
        {
            var isNegative = value < 0;

            //Make the value signed
            var unsignedValue = isNegative
                ? (ulong) -value
                : (ulong) value;

            var numChars = 1 + (isNegative ? 1 : 0);

            var temp = value;

            while ((temp /= 10) != 0)
                numChars++;

            var pos = _pos;
            var dest = _chars;

            if (pos > dest.Length - numChars)
            {
                Grow(numChars);
                dest = _chars;
            }

            if (isNegative)
            {
                dest[pos++] = '-';
                numChars--;
            }

            var current = pos + numChars - 1;

            for (var i = 0; i < numChars; i++)
            {
                dest[current - i] = (char) ('0' + (unsignedValue % 10));
                unsignedValue /= 10;
            }

            pos += numChars;
            _pos = pos;
        }

        public void Remove(int startIndex, int length)
        {
            if (length > Length - startIndex)
                throw new ArgumentOutOfRangeException();

            if (Length == length && startIndex == 0)
            {
                Length = 0;
                return;
            }

            if (Length > 0)
            {
                if (_arrayToReturnToPool != null)
                    Array.Copy(_arrayToReturnToPool, startIndex + length, _arrayToReturnToPool, startIndex, (Length - (startIndex + length)));
                else
                    _chars.Slice(startIndex + length, (Length - startIndex + length)).CopyTo(_chars.Slice(startIndex));

                Length -= length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string? s)
        {
            if (s == null)
                return;

            int pos = _pos;
            if (s.Length == 1 && (uint) pos < (uint) _chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
            {
                _chars[pos] = s[0];
                _pos = pos + 1;
            }
            else
            {
                AppendSlow(s);
            }
        }

        public void AppendRightPadded(string? s, int padRight, char c = ' ')
        {
            if (s == null)
                return;

            var requiredLength = Math.Max(s.Length, padRight);

            int pos = _pos;
            if (pos > _chars.Length - requiredLength)
            {
                Grow(requiredLength);
            }

            var chars = _chars;

            var target = chars.Slice(pos);

            s.AsSpan().CopyTo(target);

            for (var i = s.Length; i < requiredLength; i++)
                target[i] = c;

            _pos += requiredLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(StringBuilder b)
        {
            int pos = _pos;
            if (pos > _chars.Length - b.Length)
            {
                Grow(b.Length);
            }

            if (_arrayToReturnToPool != null)
                b.CopyTo(0, _arrayToReturnToPool, pos, b.Length);
            else
            {
                var chars = _chars;

                for (var i = 0; i < b.Length; i++)
                    chars[pos + i] = b[i];
            }

            _pos += b.Length;
        }

        private void AppendSlow(string s)
        {
            int pos = _pos;
            if (pos > _chars.Length - s.Length)
            {
                Grow(s.Length);
            }

            s.AsSpan().CopyTo(_chars.Slice(pos));
            _pos += s.Length;
        }

        public void Append(char c, int count)
        {
            if (_pos > _chars.Length - count)
            {
                Grow(count);
            }

            Span<char> dst = _chars.Slice(_pos, count);
            for (int i = 0; i < dst.Length; i++)
            {
                dst[i] = c;
            }
            _pos += count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(Utf16String value) => Append(value.AsSpan());

        public void Append(ReadOnlySpan<char> value)
        {
            int pos = _pos;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_chars.Slice(_pos));
            _pos += value.Length;
        }

        public unsafe void Append(FixedUtf8String value)
        {
            int pos = _pos;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            var ptr = value.Value;
            var dest = _chars;

            for (var i = 0; i < value.Length; i++)
                dest[pos++] = (char) ptr[i];

            _pos = pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<char> AppendSpan(int length)
        {
            int origPos = _pos;
            if (origPos > _chars.Length - length)
            {
                Grow(length);
            }

            _pos = origPos + length;
            return _chars.Slice(origPos, length);
        }

        public void AppendHex(ulong value, int padRight = 0)
        {
            var numChars = 1;

            var temp = value;

            while ((temp >>= 4) != 0)
                numChars++;

            var pos = _pos;
            var dest = _chars;

            var requiredLength = Math.Max(numChars, padRight);

            if (pos > dest.Length - requiredLength)
            {
                Grow(requiredLength);
                dest = _chars;
            }

            //.NET 5+'s TryFormat is no good because it doesn't tell us how big the resulting string will be, which we need to know
            //to potentially grow the buffer

            //Length prefix 0's
            for (var i = 0; i < padRight - numChars; i++)
                dest[pos++] = '0';

            //Now write the value itself. int.TryFormat to Span<char> is not available in .NET Standard 2.0

            for (var i = numChars - 1; i >= 0; i--)
            {
                var shift = i * 4;
                var val = (value >> shift) & 0xF;

                if (val < 10)
                    dest[pos++] = ((char) ('0' + val));
                else
                    dest[pos++] = ((char) (55 + val)); //'A' will be 65
            }

            _pos = pos;
        }

        //From Iced
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendHex(
            ulong value,
            int digitGroupSize,
            string? digitSeparator,
            int digits,
            bool upper,
            bool leadingZero)
        {
            if (digits == 0)
            {
                digits = 1;
                for (ulong tmp = value; ;)
                {
                    tmp >>= 4;
                    if (tmp == 0)
                        break;
                    digits++;
                }
            }

            int hexHigh = upper ? 'A' - 10 : 'a' - 10;
            if (leadingZero && digits < 17 && (int) ((value >> ((digits - 1) << 2)) & 0xF) > 9)
                digits++;// Another 0

            var pos = _pos;
            var dest = _chars;

            var numCharsNeeded = digits;

            if (digitSeparator != null)
                numCharsNeeded += (digits / digitGroupSize) * digitSeparator.Length;

            if (pos > dest.Length - numCharsNeeded)
            {
                Grow(numCharsNeeded);
                dest = _chars;
            }

            bool useDigitSep = digitGroupSize > 0 && !string.IsNullOrEmpty(digitSeparator);
            for (int i = 0; i < digits; i++)
            {
                int index = digits - i - 1;
                int digit = index >= 16 ? 0 : (int) ((value >> (index << 2)) & 0xF);
                if (digit > 9)
                    dest[pos++] = ((char) (digit + hexHigh));
                else
                    dest[pos++] = ((char) (digit + '0'));
                if (useDigitSep && index > 0 && (index % digitGroupSize) == 0)
                {
                    digitSeparator!.AsSpan().CopyTo(dest.Slice(pos));
                    pos += digitSeparator!.Length;
                }
            }

            _pos = pos;
        }

        public void Replace(char oldChar, char newChar, int startIndex, int count)
        {
            var currentLength = Length;

            if (startIndex > currentLength)
                throw new ArgumentOutOfRangeException();

            if (count < 0 || startIndex > currentLength - count)
                throw new ArgumentOutOfRangeException();

            var chars = _chars;

            for (var i = startIndex; i < startIndex + count; i++)
            {
                if (chars[i] == oldChar)
                    chars[i] = newChar;
            }
        }

        public void Clear()
        {
            _pos = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppend(char c)
        {
            Grow(1);
            Append(c);
        }

        /// <summary>
        /// Resize the internal buffer either by doubling current buffer size or
        /// by adding <paramref name="additionalCapacityBeyondPos"/> to
        /// <see cref="_pos"/> whichever is greater.
        /// </summary>
        /// <param name="additionalCapacityBeyondPos">
        /// Number of chars requested beyond current position.
        /// </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int additionalCapacityBeyondPos)
        {
            Debug.Assert(additionalCapacityBeyondPos > 0);
            Debug.Assert(_pos > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

            const uint ArrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

            // Increase to at least the required size (_pos + additionalCapacityBeyondPos), but try
            // to double the size if possible, bounding the doubling to not go beyond the max array length.
            int newCapacity = (int) Math.Max(
                (uint) (_pos + additionalCapacityBeyondPos),
                Math.Min((uint) _chars.Length * 2, ArrayMaxLength));

            // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative.
            // This could also go negative if the actual required length wraps around.
            char[] poolArray = ArrayPool<char>.Shared.Rent(newCapacity);

            _chars.Slice(0, _pos).CopyTo(poolArray);

            char[]? toReturn = _arrayToReturnToPool;
            _chars = _arrayToReturnToPool = poolArray;
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            char[]? toReturn = _arrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn);
            }
        }
    }
}
