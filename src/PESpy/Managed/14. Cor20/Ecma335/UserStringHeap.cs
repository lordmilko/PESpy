using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace PESpy
{
    public class UserStringHeap : IEnumerable<UserString> //Massively reduces memory usage
    {
        /// <summary>
        /// Gets the size of this heap in bytes.
        /// </summary>
        private int Size { get; }

        public int Offset { get; }

        private FileReader reader;

        internal UserStringHeap(ref FileReader reader, int size)
        {
            Offset = (int) reader.Position;
            Size = size;
            this.reader = reader;
        }

        internal UserString ReadString(int offset)
        {
            reader.Enter();

            try
            {
                //From II.24.2.4:

                /* Strings in the #US (user string) heap are encoded using 16-bit Unicode encodings. The count on each
                 * string is the number of bytes (not characters) in the string. Furthermore, there is an additional terminal
                 * byte (so all byte counts are odd, not even). This final byte holds the value 1 if and only if any UTF16
                 * character within the string has any bit set in its top byte, or its low byte is any of the following: 0x01–
                 * 0x08, 0x0E–0x1F, 0x27, 0x2D, 0x7F. Otherwise, it holds 0. The 1 signifies Unicode characters that
                 * require handling beyond that normally provided for 8-bit encoding sets. */

                reader.Seek(offset);

                var rawByteCount = reader.ReadCorCompressedInteger(out var compressedSize);

                if (rawByteCount > 0)
                {
                    var strByteCount = rawByteCount - 1;
                    var numChars = strByteCount / 2;

                    var str = reader.ReadUnicodeString(numChars);

                    Debug.Assert(rawByteCount % 2 == 1);

                    var unicodeByte = reader.ReadByte();

                    return new UserString(offset, compressedSize, str, unicodeByte);
                }
                else
                    return new UserString(offset, compressedSize, string.Empty, 0);
            }
            finally
            {
                reader.Exit();
            }
        }

        public IEnumerator<UserString> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private struct Enumerator : IEnumerator<UserString>
        {
            public UserString Current { get; private set; }

            object IEnumerator.Current => Current;

            private UserStringHeap userStringHeap;
            private int currentOffset;
            private int endOffset;

            internal Enumerator(UserStringHeap userStringHeap)
            {
                this.userStringHeap = userStringHeap;
                currentOffset = userStringHeap.Offset;
                endOffset = currentOffset + userStringHeap.Size;
                Current = default;
            }

            public bool MoveNext()
            {
                if (currentOffset >= endOffset)
                    return false;

                Current = userStringHeap.ReadString(currentOffset);

                currentOffset += Current.CompressedSize.Length + (Current.Value.Length == 0 ? 0 : ((Current.Value.Length * 2) + 1));
                return true;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
