using System;
using System.Runtime.CompilerServices;

namespace PESpy
{
    //Encapsulates an offset into a MemoryBlock
    internal readonly unsafe struct MemoryChunk
    {
        //The offset from the block's pointer that this chunk encapsulates.
        public readonly int RelativeOffset;

        public int AbsoluteOffset
        {
            get
            {
                if (block == null)
                    return 0;

                return block.GetAbsoluteOffset(RelativeOffset);
            }
        }

        public byte* Pointer => block.LocalPointer + RelativeOffset;

        /// <summary>
        /// Gets the number of bytes remaining in this chunk's underlying block relative to the <see cref="RelativeOffset"/> of this chunk.
        /// </summary>
        public int Remaining => block.Length - RelativeOffset;

        //length is the length remaining in the MemoryBlock after subtracting our offset
        internal readonly MemoryBlock block;

        public bool Is32Bit => block.Is32Bit;
        public int PointerSize => block.Is32Bit ? 4 : 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte(int offset) => *(byte*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short PeekInt16(int offset) => *(short*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort PeekUInt16(int offset) => *(ushort*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekInt32(int offset) => *(int*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint PeekUInt32(int offset) => *(uint*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long PeekInt64(int offset) => *(long*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong PeekUInt64(int offset) => *(ulong*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong PeekPointer(int offset) => Is32Bit ? PeekUInt32(offset) : PeekUInt64(offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> PeekSpan<T>(int offset, int numElems) => new Span<T>(Pointer + offset, numElems);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Guid PeekGuid(int offset) => *(Guid*) (Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String PeekNullPaddedUtf8(int offset, int numChars) => new Utf8String(Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnsiString PeekAnsiNullTerminatedString(int offset) => new AnsiString(Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8String PeekUtf8NullTerminatedString(int offset) => new Utf8String(Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf16String PeekUtf16NullTerminatedString(int offset) => new Utf16String((char*) (Pointer + offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedAnsiString PeekAnsiFixedLength(int offset, int numChars) => new FixedAnsiString(Pointer + offset, numChars);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedUtf8String PeekUtf8FixedLength(int offset, int numChars) => new FixedUtf8String(Pointer + offset, numChars);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedUtf16String PeekUtf16FixedLength(int offset, int numChars) => new FixedUtf16String((char*) (Pointer + offset), numChars);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekBigEndianInt32(int offset)
        {
            var ptr = (Pointer + offset);

            return (ptr[0] << 24) | (ptr[1] << 16) | (ptr[2] << 8) | ptr[3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekSpacePaddedInt32(int offset, int numChars)
        {
            var ptr = (Pointer + offset);

            var span = new Span<byte>(ptr, numChars);
            var firstSpace = span.IndexOf((byte) 0x20); //Space

            var end = firstSpace == -1 ? numChars : firstSpace;

            var value = 0;

            for (var i = 0; i < end; i++)
            {
                value = value * 10 + (span[i] - (byte) '0');
            }

            return value;
        }

        #region Try

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort TryPeekUInt16(int offset, int ownerLength) => offset < ownerLength ? PeekUInt16(offset) : (ushort) 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TryPeekInt32(int offset, int ownerLength) => offset < ownerLength ? PeekInt32(offset) : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint TryPeekUInt32(int offset, int ownerLength) => offset < ownerLength ? PeekUInt32(offset) : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long TryPeekPointer(int offset, int ownerLength) => offset < ownerLength ? (long) PeekPointer(offset) : 0;

        #endregion

        internal MemoryChunk Slice(int offset)
        {
            if ((uint) offset > (uint) Remaining)
                throw new InvalidOperationException("Attempted to slice beyond the end of a block");

            return new MemoryChunk(block, this.RelativeOffset + offset);
        }

        internal void Demand(int rva, int length) => block.Demand(rva, length);

        public MemoryChunk(MemoryBlock block, int offset)
        {
            if (!block.Contains(offset))
                throw new InvalidOperationException($"{nameof(MemoryChunk)} does not contain offset '{offset}'");

            this.block = block;
            RelativeOffset = offset;
        }
    }
}
