using System;
using System.Runtime.CompilerServices;
using System.Text;
using ClrDebug;

namespace PESpy
{
    public unsafe struct ByteReader
    {
        internal const int InvalidCompressedInteger = int.MaxValue;

        private static ReadOnlySpan<CorTokenType> g_tkCorEncodeToken => new[]
        {
            CorTokenType.mdtTypeDef,
            CorTokenType.mdtTypeRef,
            CorTokenType.mdtTypeSpec,
            CorTokenType.mdtBaseType
        };

        private readonly byte* startPointer;
        private readonly byte* endPointer;
        private byte* currentPointer;

        public byte* CurrentPointer => currentPointer;

        public int Length => (int) (endPointer - startPointer);

        public int Offset
        {
            get => (int) (currentPointer - startPointer);
            set => currentPointer = startPointer + value;
        }

        public int RemainingBytes => (int) (endPointer - currentPointer);

        public ByteReader(byte* buffer, int length)
        {
            startPointer = buffer;
            currentPointer = buffer;
            endPointer = buffer + length;
        }

        public ByteReader(NativeSpan<byte> bytes) : this((byte*) bytes, bytes.Length)
        {
        }

        public bool ReadBoolean() => ReadByte() != 0;

        public char ReadChar()
        {
            byte* ptr = Read(sizeof(char));
            return (char) (ptr[0] + (ptr[1] << 8));
        }

        public byte ReadByte() => *Read1();

        public ushort ReadUInt16()
        {
            var ptr = Read(sizeof(ushort));
            return (ushort) unchecked((ptr[0] + (ptr[1] << 8)));
        }

        public uint ReadUInt32()
        {
            byte* ptr = Read(sizeof(uint));
            return (uint) (ptr[0] + (ptr[1] << 8) + (ptr[2] << 16) + (ptr[3] << 24));
        }

        public ulong ReadUInt64() => unchecked((ulong) ReadInt64());

        public sbyte ReadSByte() => *(sbyte*) Read1();

        public ushort ReadInt16()
        {
            byte* ptr = Read(sizeof(short));

            unchecked
            {
                return (ushort) (ptr[0] + (ptr[1] << 8));
            }
        }

        public int ReadInt32()
        {
            byte* ptr = Read(sizeof(int));

            return unchecked((int) (ptr[0] + (ptr[1] << 8) + (ptr[2] << 16) + (ptr[3] << 24)));
        }

        public long ReadInt64()
        {
            byte* ptr = Read(sizeof(long));

            unchecked
            {
                uint lo = (uint) (ptr[0] + (ptr[1] << 8) + (ptr[2] << 16) + (ptr[3] << 24));
                uint hi = (uint) (ptr[4] + (ptr[5] << 8) + (ptr[6] << 16) + (ptr[7] << 24));
                return (long) (lo + ((ulong) hi << 32));
            }
        }

        public float ReadSingle()
        {
            int val = ReadInt32();
            return *(float*) &val;
        }

        public double ReadDouble()
        {
            long val = ReadInt64();
            return *(double*) &val;
        }

        public FixedUtf8String ReadSerString()
        {
            var length = ReadCompressedIntegerOrInvalid();

            if (length != int.MaxValue)
            {
                var str = new FixedUtf8String(currentPointer, length);
                currentPointer += length;
                return str;
            }

            //0xFF indicates a null string
            if (ReadByte() != 0xFF)
                throw new InvalidOperationException("Invalid serialized string");

            return default;
        }

        public FixedUtf8String ReadSerString(int offset)
        {
            Offset = offset;

            return ReadSerString();
        }

        public CorElementType ReadCorElementType() =>
            (CorElementType) ReadCompressedIntegerOrInvalid();

        public CorSerializationType ReadSerializationType() =>
            (CorSerializationType) ReadCompressedIntegerOrInvalid();

        public mdToken ReadToken()
        {
            var value = ReadCompressedIntegerOrInvalid();
            var tokenType = g_tkCorEncodeToken[(int) (value & 0x3)];

            if (value == int.MaxValue)
                return default;

            return Extensions.TokenFromRid(value >> 2, tokenType);
        }

        public int ReadCompressedInteger()
        {
            var result = ReadCompressedIntegerOrInvalid();

            if (result == int.MaxValue)
                throw new InvalidOperationException("Invalid compressed integer");

            return result;
        }

        private int ReadCompressedIntegerOrInvalid()
        {
            byte* ptr = currentPointer;
            long limit = RemainingBytes;

            if (limit == 0)
            {
                return InvalidCompressedInteger;
            }

            byte headerByte = ptr[0];
            if ((headerByte & 0x80) == 0)
            {
                currentPointer += 1;
                return headerByte;
            }
            else if ((headerByte & 0x40) == 0)
            {
                if (limit >= 2)
                {
                    currentPointer += 2;
                    return ((headerByte & 0x3f) << 8) | ptr[1];
                }
            }
            else if ((headerByte & 0x20) == 0)
            {
                if (limit >= 4)
                {
                    currentPointer += 4;
                    return ((headerByte & 0x1f) << 24) | (ptr[1] << 16) | (ptr[2] << 8) | ptr[3];
                }
            }

            return InvalidCompressedInteger;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* Read1()
        {
            byte* p = currentPointer;

            if (p >= endPointer)
                throw new InvalidOperationException("Attempted to read beyond the length of the buffer");

            currentPointer = p + 1;

            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte* Read(int length)
        {
            var p = currentPointer;

            if (unchecked((uint) length) > (uint) (endPointer - p))
                throw new InvalidOperationException("Attempted to read beyond the length of the buffer");

            currentPointer = p + length;

            return p;
        }
    }
}
