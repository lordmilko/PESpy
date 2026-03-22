using System;
using System.IO;

namespace PESpy
{
    internal class FileWriter
    {
        private Stream _stream;

        public int Position => (int) _stream.Position;

        public FileWriter(Stream stream)
        {
            _stream = stream;
        }

        public void Seek(int offset) => _stream.Position = offset;

        public void Skip(int count) => _stream.Position += count;

        public void WriteBytes(byte[] bytes)
        {
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteByte(byte value) =>
            _stream.WriteByte(value);

        public void WriteUInt16(ushort value)
        {
            _stream.WriteByte((byte)(value));
            _stream.WriteByte((byte)(value >> 8));
        }

        public void WriteUInt32(uint value)
        {
            _stream.WriteByte((byte) (value));
            _stream.WriteByte((byte) (value >> 8));
            _stream.WriteByte((byte) (value >> 16));
            _stream.WriteByte((byte) (value >> 24));
        }

        public void WriteUInt32At(uint value, int position)
        {
            var oldPos = Position;
            Seek(position);
            WriteUInt32(value);
            Seek(oldPos);
        }

        public void WriteBigEndianInt32(int value)
        {
            _stream.WriteByte((byte) (value >> 24));
            _stream.WriteByte((byte) (value >> 16));
            _stream.WriteByte((byte) (value >> 8));
            _stream.WriteByte((byte) value);
        }

        public void WriteFixedAnsiString(string value)
        {
            for (var i = 0; i < value.Length; i++)
                _stream.WriteByte((byte) value[i]);
        }

        public void WriteFixedUtf8String(Span<byte> value)
        {
            for (var i = 0; i < value.Length; i++)
                _stream.WriteByte((byte) value[i]);
        }

        public void WriteNullPaddedUtf8(string value, int length)
        {
            WriteFixedAnsiString(value);

            for (var i = value.Length; i < length; i++)
                _stream.WriteByte(0);
        }

        public void WriteNullTerminatedAnsiString(string value)
        {
            WriteFixedAnsiString(value);
            _stream.WriteByte(0);
        }

        public void WriteSpacePaddedAnsiString(string value, int length)
        {
            WriteFixedAnsiString(value);

            for (var i = value.Length; i < length; i++)
                _stream.WriteByte((byte) ' ');
        }

        public void WriteSpacePaddedInt32(int? value, int length, int @base =10)
        {
            if (value == null)
            {
                for (var i = 0; i < length; i++)
                    _stream.WriteByte((byte) ' ');

                return;
            }

            var val = value.Value;

            var isNegative = false;

            if (val < 0)
            {
                isNegative = true;
                val = -val; //Strip off the negative sign
            }

            Span<byte> chars = stackalloc byte[32];
            var pos = chars.Length;

            do
            {
                var digit = val % @base;
                val /= @base;
                chars[--pos] = (byte) ('0' + digit);
            } while (val != 0);

            if (isNegative)
                chars[--pos] = (byte) '-';

            for (var i = pos; i < chars.Length; i++)
                _stream.WriteByte(chars[i]);

            var numChars = chars.Length - pos;

            for (var i = numChars; i < length; i++)
                _stream.WriteByte((byte) ' ');
        }

        public void Align(int target, byte b)
        {
            var alignedSize = ((int) Position + (target - 1)) & (~(target - 1));

            for (var i = Position; i < alignedSize; i++)
                _stream.WriteByte(b);
        }
    }
}
