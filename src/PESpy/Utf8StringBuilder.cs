using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PESpy
{
    public ref struct Utf8StringBuilder
    {
        private byte[]? _arrayToReturnToPool;
        private Span<byte> _chars;
        private int _pos;

        public Utf8StringBuilder(Span<byte> initialBuffer)
        {
            _arrayToReturnToPool = null;
            _chars = initialBuffer;
            _pos = 0;
        }

        public Utf8StringBuilder(int initialCapacity)
        {
            _arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(initialCapacity);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string? s)
        {
            if (s == null)
            {
                return;
            }

            int pos = _pos;
            if (s.Length == 1 && (uint) pos < (uint) _chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
            {
                _chars[pos] = (byte) s[0];
                _pos = pos + 1;
            }
            else
            {
                AppendSlow(s);
            }
        }

        public void Append(char c)
        {
            if (_pos < _chars.Length)
                _chars[_pos++] = (byte) c;
            else
            {
                Grow(1);

                _chars[_pos++] = (byte) c;
            }
        }

        public void Append(long value)
        {
            //IUtf8SpanFormattable is no good; it doesn't tell you the total number of bytes that would be needed if the buffer isn't big enough

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
                dest[pos++] = (byte) '-';
                numChars--;
            }

            var current = pos + numChars - 1;

            for (var i = 0; i < numChars; i++)
            {
                dest[current - i] = (byte) ('0' + (unsignedValue % 10));
                unsignedValue /= 10;
            }

            pos += numChars;
            _pos = pos;
        }

        public void Append(ulong value)
        {
            var numChars = 1;

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

            var current = pos + numChars - 1;

            for (var i = 0; i < numChars; i++)
            {
                dest[current - i] = (byte) ('0' + (value % 10));
                value /= 10;
            }

            pos += numChars;
            _pos = pos;
        }

        public void Append(FixedUtf8String value)
        {
            int pos = _pos;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_chars.Slice(_pos));
            _pos += value.Length;
        }

        public void Append(SymString value)
        {
            int pos = _pos;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_chars.Slice(_pos));
            _pos += value.Length;
        }

        public void Append(Utf8String value)
        {
            int pos = _pos;
            if (pos > _chars.Length - value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_chars.Slice(_pos));
            _pos += value.Length;
        }

        private unsafe void AppendSlow(string s)
        {
            int pos = _pos;

            var maxBytes = Encoding.UTF8.GetMaxByteCount(s.Length);

            if (pos > _chars.Length - maxBytes)
            {
                Grow(maxBytes);
            }

            fixed (byte* b = _chars.Slice(pos))
            fixed (char* c = s)
            {
                var actual = Encoding.UTF8.GetBytes(c, s.Length, b, maxBytes);

                _pos += actual;
            }
        }

        public void CopyTo(Span<byte> span) => throw new NotImplementedException();

        public char this[int index] => (char)  _chars[index];

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
            byte[] poolArray = ArrayPool<byte>.Shared.Rent(newCapacity);

            _chars.Slice(0, _pos).CopyTo(poolArray);

            byte[]? toReturn = _arrayToReturnToPool;
            _chars = _arrayToReturnToPool = poolArray;
            if (toReturn != null)
            {
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }

        public unsafe FixedUtf8String ToPointer()
        {
            var buffer = Marshal.AllocHGlobal(_pos);
            _chars.Slice(0, _pos).CopyTo(new Span<byte>((void*) buffer, Length));
            return new FixedUtf8String((byte*) buffer, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            byte[]? toReturn = _arrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null)
            {
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }

        public void Clear() => _pos = 0;

        public override unsafe string ToString()
        {
            fixed (byte* b = _chars)
                return Encoding.UTF8.GetString(b, _pos);
        }
    }
}
