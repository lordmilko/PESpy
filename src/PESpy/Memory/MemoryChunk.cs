#if PEFAST
using System;
using System.Runtime.CompilerServices;

namespace PESpy
{
    //Encapsulates an offset into a MemoryBlock
    internal readonly unsafe struct MemoryChunk
    {
        //The offset from the block's pointer that this chunk encapsulates.
        public readonly int BlockOffset;

        public int AbsoluteOffset => block.RemoteStartOffset + BlockOffset;

        public byte* Pointer => block.LocalPointer + BlockOffset;

        public PEFile PEFile => block.Provider.PEFile;

        //length is the length remaining in the MemoryBlock after subtracting our offset
        private readonly MemoryBlock block;

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
        public Utf8String PeekNullPaddedUTF8(int offset, int numChars) => new Utf8String(Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AnsiString PeekAnsiNullTerminatedString(int offset) => new AnsiString(Pointer + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> PeekUnicodeFixedLength(int numChars) => new ReadOnlySpan<char>(Pointer + AbsoluteOffset, numChars);

        internal MemoryChunk Slice(int offset) => new MemoryChunk(block, this.BlockOffset + offset);

        internal void Demand(int rva, int length) => block.Demand(rva, length);

        public MemoryChunk(MemoryBlock block, int offset)
        {
            this.block = block;
            BlockOffset = offset;
        }
    }
}
#endif
