using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PESpy
{
    /// <summary>
    /// Represents a <see cref="Stream"/> capable of eagerly filling a buffer in preparation for reading several small values from its data target.
    /// </summary>
    public interface IBufferableStream
    {
        void FillBuffer(int count);
    }

    public class StreamFileReader : IFileReader, IDisposable
    {
        //I expect most strings should be able to fit within 100 characters
        private const int DefaultStringCapacity = 100;

        //This must not be exposed, as whenever we seek we need to do so relative to the start position
        private Stream stream;
        private IBufferableStream? bufferableStream;

        private long start;
        private object readerLock;
        private StringBuilder stringBuilder = new StringBuilder(DefaultStringCapacity);
        private List<byte> byteList = new List<byte>(DefaultStringCapacity);

        //A buffer that stores the bytes for the current value being read
        private byte[] valueBuffer;

        public long Position => stream.Position - start;

        public StreamFileReader(Stream stream, bool seekRelativeToCurrentPosition, object? readerLock = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            this.stream = stream;
            bufferableStream = stream as IBufferableStream;

            if (seekRelativeToCurrentPosition)
                this.start = stream.Position;
            else
                this.start = 0;

            this.readerLock = readerLock ?? new object();
            valueBuffer = new byte[16];
        }

        internal StreamFileReader(Stream stream, object readerLock) : this(stream, true)
        {
            this.readerLock = readerLock;
            valueBuffer = new byte[16];
        }

        internal StreamFileReader(Stream stream, object readerLock, long start, byte[] valueBuffer) : this(stream, readerLock)
        {
            this.readerLock = readerLock;
            this.start = start;
            this.valueBuffer = valueBuffer;
        }

        public void FillBuffer(int count)
        {
            bufferableStream?.FillBuffer(count);
        }

        public void Seek(long offset)
        {
            stream.Seek(offset + start, SeekOrigin.Begin);
        }

        public void SkipBytes(long count)
        {
            stream.Seek(count, SeekOrigin.Current);
        }

        void IFileReader.Enter() => Monitor.Enter(readerLock);
        void IFileReader.Exit() => Monitor.Exit(readerLock);

#if DEBUG_POSITION
        public void Seek(RawOffset offset)
        {
            stream.Seek((int) offset + start, SeekOrigin.Begin);
        }
#endif

        public Guid ReadGuid()
        {
            FillValueBuffer(16);

            unchecked
            {
                //Don't allocate a byte[], consume the constituent components of the Guid manually
                return new Guid(
                    (int)(valueBuffer[0] | (valueBuffer[1] << 8) | (valueBuffer[2] << 16) | (valueBuffer[3] << 24)),
                    (short)(valueBuffer[4] | (valueBuffer[5] << 8)),
                    (short)(valueBuffer[6] | (valueBuffer[7] << 8)),
                    valueBuffer[8], valueBuffer[9], valueBuffer[10], valueBuffer[11], valueBuffer[12], valueBuffer[13], valueBuffer[14], valueBuffer[15]);
            }
        }

        public unsafe T[] ReadArray<T>(int count) where T : unmanaged
        {
            if (count == 0)
                return Array.Empty<T>();

            var size = Marshal.SizeOf<T>();

            if (size > valueBuffer.Length)
                throw new InvalidOperationException($"Cannot read an array of type {typeof(T).Name}: each element is larger than the size of our internal buffer");

            var totalSize = count * size;

            var array = new T[count];

            if (totalSize < valueBuffer.Length)
            {
                FillValueBuffer(totalSize);

                fixed (byte* p = valueBuffer)
                {
                    for (var i = 0; i < count; i++)
                        array[i] = *(T*)(p + (i * size));
                }
            }
            else
            {
                //Chunk it

                var maxItemsPerFill = valueBuffer.Length / size;

                var index = 0;

                while (index < count)
                {
                    var remaining = count - index;
                    var required = Math.Min(maxItemsPerFill, remaining);

                    FillValueBuffer(required * size);

                    fixed (byte* p = valueBuffer)
                    {
                        for (var i = 0; i < required; i++, index++)
                            array[index] = *(T*) (p + (i *size));
                    }
                }
            }

            return array;
        }

        public string ReadAnsiNullTerminatedString()
        {
            Debug.Assert(stringBuilder.Length == 0);

            while (true)
            {
                byte b = ReadByte();

                if (0 == b)
                    break;

                stringBuilder.Append((char)b);
            }

            var result = stringBuilder.ToString();

            stringBuilder.Clear();

            return result;
        }

        public string ReadUTF8NullTerminatedString()
        {
            Debug.Assert(byteList.Count == 0);

            while (true)
            {
                byte b = ReadByte();

                if (0 == b)
                    break;

                byteList.Add(b);
            }

            var str = Encoding.UTF8.GetString(byteList.ToArray());

            byteList.Clear();

            return str;
        }

        public string ReadUTF16NullTerminatedString()
        {
            Debug.Assert(byteList.Count == 0);

            while (true)
            {
                byte b1 = ReadByte();
                byte b2 = ReadByte();

                if (0 == b1 && 0 == b2)
                    break;

                byteList.Add(b1);
                byteList.Add(b2);
            }

            var str = Encoding.Unicode.GetString(byteList.ToArray());

            byteList.Clear();

            return str;
        }

        public bool TryMatchAnsiNullTerminatedString(string value)
        {
            return ReadAnsiNullTerminatedString() == value;
        }

        public byte ReadByte()
        {
            var b = stream.ReadByte();

            if (b == -1)
            {
                //Some things will try and read bytes until a limit has been reached; thus, we must always throw

                throw new BadImageFormatException("Failed to read a byte. Image may have been unloaded");
            }

            return (byte) b;
        }

        public sbyte ReadSByte()
        {
            throw new NotImplementedException();
        }

        public short ReadInt16()
        {
            FillValueBuffer(2);
            return (short)(valueBuffer[0] | valueBuffer[1] << 8);
        }

        public ushort ReadUInt16()
        {
            FillValueBuffer(2);
            return (ushort)(valueBuffer[0] | valueBuffer[1] << 8);
        }

        public int ReadInt32()
        {
            FillValueBuffer(4);
            return (int)(valueBuffer[0] | valueBuffer[1] << 8 | valueBuffer[2] << 16 | valueBuffer[3] << 24);
        }

        public uint ReadUInt32()
        {
            FillValueBuffer(4);
            return (uint)(valueBuffer[0] | valueBuffer[1] << 8 | valueBuffer[2] << 16 | valueBuffer[3] << 24);
        }

        public int Read7BitEncodedInt32()
        {
            // Unlike writing, we can't delegate to the 64-bit read on
            // 64-bit platforms. The reason for this is that we want to
            // stop consuming bytes if we encounter an integer overflow.

            uint result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 5 bytes,
            // or the fifth byte is about to cause integer overflow.
            // This means that we can read the first 4 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 4;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int) result; // early exit
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.

            byteReadJustNow = ReadByte();
            if (byteReadJustNow > 0b_1111u)
            {
                //throw new FormatException(SR.Format_Bad7BitInt);
                throw new NotImplementedException(); //todo
            }

            result |= (uint) byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int) result;
        }

        public long ReadInt64()
        {
            FillValueBuffer(8);
            uint lo = (uint)(valueBuffer[0] | valueBuffer[1] << 8 | valueBuffer[2] << 16 | valueBuffer[3] << 24);
            uint hi = (uint)(valueBuffer[4] | valueBuffer[5] << 8 | valueBuffer[6] << 16 | valueBuffer[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        public ulong ReadUInt64()
        {
            FillValueBuffer(8);
            uint lo = (uint)(valueBuffer[0] | valueBuffer[1] << 8 | valueBuffer[2] << 16 | valueBuffer[3] << 24);
            uint hi = (uint)(valueBuffer[4] | valueBuffer[5] << 8 | valueBuffer[6] << 16 | valueBuffer[7] << 24);
            return ((ulong)hi) << 32 | lo;
        }

        public bool TryReadInt32(out int value)
        {
            if (TryFillValueBuffer(4))
            {
                value = (int) (valueBuffer[0] | valueBuffer[1] << 8 | valueBuffer[2] << 16 | valueBuffer[3] << 24);
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryReadInt64(out long value)
        {
            if (TryFillValueBuffer(8))
            {
                uint lo = (uint)(valueBuffer[0] | valueBuffer[1] << 8 | valueBuffer[2] << 16 | valueBuffer[3] << 24);
                uint hi = (uint)(valueBuffer[4] | valueBuffer[5] << 8 | valueBuffer[6] << 16 | valueBuffer[7] << 24);
                value = (long)((ulong)hi) << 32 | lo;

                return true;
            }

            value = 0;
            return false;
        }

        public float ReadFloat()
        {
            throw new NotImplementedException();
        }

        public double ReadDouble()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads a fixed-length byte block as a null-padded UTF8-encoded string.
        /// The padding is not included in the returned string.
        ///
        /// Note that it is legal for UTF8 strings to contain NUL; if NUL occurs
        /// between non-NUL codepoints, it is not considered to be padding and
        /// is included in the result.
        /// </summary>
        public string ReadNullPaddedUTF8(int byteCount)
        {
            byte[] bytes = ReadBytes(byteCount);
            int nonPaddedLength = 0;

            for (int i = bytes.Length; i > 0; --i)
            {
                if (bytes[i - 1] != 0)
                {
                    nonPaddedLength = i;
                    break;
                }
            }

            return Encoding.UTF8.GetString(bytes, 0, nonPaddedLength);
        }

        //String may or may not be null terminated. If it is, we'll trim the trailing null
        public string ReadUnicodeString(int charCount)
        {
            if (charCount == 0)
                return string.Empty;

            Debug.Assert(stringBuilder.Length == 0);

            for (var i = 0; i < charCount; i++)
                stringBuilder.Append((char) ReadUInt16());

            if (stringBuilder[stringBuilder.Length - 1] == '\0')
                stringBuilder.Length--;

            var str = stringBuilder.ToString();

            stringBuilder.Clear();

            return str;
        }

        public string ReadAsciiString(int charCount)
        {
            if (charCount == 0)
                return string.Empty;

            var bytes = ReadBytes(charCount);

            return Encoding.ASCII.GetString(bytes);
        }

        public string ReadUTF8String(int charCount)
        {
            if (charCount == 0)
                return string.Empty;

            var bytes = ReadBytes(charCount);

            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");

            if (count == 0)
                return Array.Empty<byte>();

            var result = new byte[count];

            var read = stream.Read(result, 0, count);

            if (read != count)
            {
                //Some things will try and read bytes until a limit has been reached; thus, we must always throw

                throw new BadImageFormatException($"Attempted to read {count} bytes however only {read} were read. Image may have been unloaded");
            }

            return result;
        }

        public bool TryReadBytes(int count, out byte[]? bytes)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");

            if (count == 0)
            {
                bytes = Array.Empty<byte>();
                return true;
            }

            var result = new byte[count];

            var read = stream.Read(result, 0, count);

            if (read == 0)
            {
                bytes = null;
                return false;
            }

            //Whatever we read, good enough!
            bytes = result;
            return true;
        }

        private void FillValueBuffer(int numBytes)
        {
            if (!TryFillValueBuffer(numBytes))
            {
                //Some things will try and read bytes until a limit has been reached; thus, we must always throw

                throw new BadImageFormatException(); //todo: error message
            }
        }

        private bool TryFillValueBuffer(int numBytes)
        {
            if (numBytes < 0 || numBytes > valueBuffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(numBytes), "The number of bytes requested does not fit into FileReader's internal buffer.");
            }
            int bytesRead = 0;
            int n = 0;

            if (stream == null)
                throw new NotImplementedException();

            // Need to find a good threshold for calling ReadByte() repeatedly
            // vs. calling Read(byte[], int, int) for both buffered & unbuffered
            // streams.
            if (numBytes == 1)
            {
                n = stream.ReadByte();
                if (n == -1)
                    throw new NotImplementedException();

                valueBuffer[0] = (byte)n;
                return true;
            }

            do
            {
                n = stream.Read(valueBuffer, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {
                    return false;
                }
                bytesRead += n;
            } while (bytesRead < numBytes);

            return true;
        }

        #region ECMA-335

        public int ReadCorCompressedInteger(out byte[] bytes)
        {
            var byte1 = ReadByte();

            //If the first one byte of the 'blob' is 0bbbbbbb2, then the rest of the 'blob' contains the bbbbbbb2 bytes of actual data
            //That is to say, if the high bit is 0, the low 7 bits contain the number. Since the high bit is 0, there's no problem
            if ((byte1 & 0x80) == 0) //10000000 
            {
                bytes = new[] { byte1 };
                return byte1;
            }

            //If the first two bytes of the 'blob' are 10bbbbbb2 and x, then the rest of the 'blob' contains the(bbbbbb2 << 8 + x) bytes of actual data.
            //Based on the check above, the high bit was not 0, so it's 1. If the second bit is not 1, then you can get the number from the bottom 6 bytes combined with the second byte
            if ((byte1 & 0x40) == 0) //01000000
            {
                var byte2 = ReadByte();

                bytes = new[] {byte1, byte2};

                //0x3F: 00111111
                //Get the bottom 6 bits (the second top being 1 from the first check failing should be ignored), left shift 8 and combine with the second bit
                return ((byte1 & 0x3f) << 8) | byte2;
            }

            if ((byte1 & 0x20) == 0) //00100000
            {
                var byte2 = ReadByte();
                var byte3 = ReadByte();
                var byte4 = ReadByte();

                bytes = new[] {byte1, byte2, byte3, byte4};

                //0x1F: 00011111
                //Get the bottom 5 bits (the second top bit being 1 from the second check failing should be ignored), and then shift each bit into position
                return ((byte1 & 0x1f) << 24) | (byte2 << 16) | (byte3 << 8) | byte4;
            }

            throw new InvalidOperationException("Failed to read an ECMA 335 compressed integer");
        }

        #endregion

        internal Stream GetStreamStartUnsafe()
        {
            //Rewind to the start first
            stream.Seek(start, SeekOrigin.Begin);

            return stream;
        }

        internal Stream GetStreamUnsafe() => stream;

        SectionReader IFileReader.CreateSectionReader(int length)
        {
            return new SectionReader(stream, readerLock, start, length, valueBuffer);
        }

        internal (StreamFileReader, object) CreateSubReader()
        {
            return (new StreamFileReader(stream, readerLock, Position, valueBuffer), readerLock);
        }

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
