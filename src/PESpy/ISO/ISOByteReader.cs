using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PESpy.ISO
{
    internal unsafe struct ISOByteReader
    {
        internal byte* _buffer;
        private byte* _end;

        public int Remaining => (int) (_end - _buffer);

        public ISOByteReader(byte* buffer, int length)
        {
            _buffer = buffer;
            _end = buffer + length;
        }

        public byte ReadByte()
        {
            return *_buffer++;
        }

        public string ReadString(int length)
        {
            var str = Encoding.ASCII.GetString(_buffer, length);
            _buffer += length;
            return str;
        }

        //Wile there are multiple types of characters (a-chars, d-chars, etc) they're ultimately
        //still just strings
        public unsafe string ReadChars(int start, int end, Encoding encoding)
        {
            var length = (end - start) + 1;

            if (length == 1)
            {
                //This is probably either parent or self; we can't be doing length / sizeof(T)
                var ch = (char) *_buffer++;
                return ch.ToString();
            }

            if (encoding == Encoding.ASCII)
                return ReadChars<byte>(length, (byte) ' ', encoding);
            else
                return ReadChars<short>(length, 0x2000, encoding); //It's big endian so we can't just do 0x20 (space)
        }

        private string ReadChars<T>(int length, T space, Encoding encoding) where T : unmanaged, IEquatable<T>
        {
            var span = new Span<T>(_buffer, length / sizeof(T));
            _buffer += length;
#if NET
            span = span.Trim(space);
#else
            var i = 0;

            for (; i < span.Length; i++)
            {
                if (!span[i].Equals(space))
                    break;
            }

            span = span.Slice(i);

            if (span.Length == 0)
                return null;

            i = span.Length - 1;

            for (; i >= 0; i--)
            {
                if (!span[i].Equals(space))
                    break;
            }

            span = span.Slice(0, i + 1);
#endif

            if (span.Length == 0)
                return null;

            fixed (byte* pBytes = MemoryMarshal.AsBytes(span))
            {
                return encoding.GetString(pBytes, span.Length * sizeof(T));
            }
        }

        public ushort ReadBothUInt16(int start, int end)
        {
            var length = (end - start) + 1;

            Debug.Assert(length == 4);

            var little = ReadLittleEndianUInt16(start, start + 1);
            var big = ReadBigEndianUInt16(start + 2, end);

            //I've had cases where this assert fired and there literally _was_ a difference in the bytes
            //Debug.Assert(little == big);

            return little;
        }

        public ushort ReadLittleEndianUInt16(int start, int end)
        {
            var length = (end - start) + 1;
            Debug.Assert(length == 2);

            var ptr = _buffer;

            _buffer += length;

            return (ushort) (ptr[0] | ptr[1] << 8);
        }

        public ushort ReadBigEndianUInt16(int start, int end)
        {
            var length = (end - start) + 1;
            Debug.Assert(length == 2);

            var ptr = _buffer;

            _buffer += length;

            return (ushort) (ptr[0] << 8 | ptr[1]);
        }

        public uint ReadBothUInt32(int start, int end)
        {
            var length = (end - start) + 1;
            Debug.Assert(length == 8);

            var little = ReadLittleEndianUInt32(start, start + 3);
            var big = ReadBigEndianUInt32(start + 4, end);

            Debug.Assert(little == big);

            return little;
        }

        public uint ReadLittleEndianUInt32(int start, int end)
        {
            var length = (end - start) + 1;
            Debug.Assert(length == 4);

            var ptr = _buffer;

            _buffer += length;

            return (uint) (ptr[0] | ptr[1] << 8 | ptr[2] << 16 | ptr[3] << 24);
        }

        public uint ReadBigEndianUInt32(int start, int end)
        {
            var length = (end - start) + 1;
            Debug.Assert(length == 4);

            var ptr = _buffer;

            _buffer += length;

            return (uint) (ptr[0] << 24 | ptr[1] << 16 | ptr[2] << 8 | ptr[3]);
        }

        public DateTime? ReadDateTime(int start, int end)
        {
            var length = (end - start) + 1;

            var ptr = _buffer;

            //In Microsoft ISOs you can have "0" on all bytes when a DateTime value is not present

            var year = 1900 + ptr[0];
            var month = ptr[1];
            var day = ptr[2];
            var hour = ptr[3];
            var minute = ptr[4];
            var second = ptr[5];

            try
            {
                if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1)
                    return null;

                if (day > DateTime.DaysInMonth(year, month))
                    return null;

                DateTime date = new DateTime(
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    second,
                    DateTimeKind.Utc
                );

                var result = (date - TimeSpan.FromMinutes(15 * ptr[6])).ToLocalTime();

                return result;
            }
            finally
            {
                _buffer += length;
            }
        }

        public void SkipByte()
        {
            _buffer++;
        }

        public void SkipByte(int start, int end)
        {
            var length = (end - start) + 1;

            _buffer += length;
        }

        public byte[] ReadArray(int length)
        {
            var result = new Span<byte>(_buffer, length).ToArray();
            _buffer += length;
            return result;
        }

        public NativeSpan<byte> ReadArray(int start, int end)
        {
            var length = (end - start) + 1;

            var result = new NativeSpan<byte>(_buffer, length);

            _buffer += length;

            return result;
        }
    }
}
