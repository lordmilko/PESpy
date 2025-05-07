using System;

namespace PESpy
{
    internal interface IFileReader
    {
        long Position { get; }

        void FillBuffer(int count);
        void Seek(long offset);
        void SkipBytes(long count);

        void Enter();
        void Exit();

        Guid ReadGuid();

        T[] ReadArray<T>(int count) where T : unmanaged;

        string ReadAnsiNullTerminatedString();
        string ReadUTF8NullTerminatedString();
        string ReadUTF16NullTerminatedString();

        bool TryMatchAnsiNullTerminatedString(string value);

        byte ReadByte();
        sbyte ReadSByte();

        short ReadInt16();
        ushort ReadUInt16();

        int ReadInt32();
        uint ReadUInt32();

        int Read7BitEncodedInt32();

        long ReadInt64();
        ulong ReadUInt64();

        bool TryReadInt32(out int value);
        bool TryReadInt64(out long value);

        float ReadFloat();
        double ReadDouble();

        string ReadNullPaddedUTF8(int byteCount);
        string ReadUnicodeString(int charCount);
        string ReadAsciiString(int charCount);
        string ReadUTF8String(int charCount);
        byte[] ReadBytes(int count);
        bool TryReadBytes(int count, out byte[]? bytes);

        int ReadCorCompressedInteger(out byte[] bytes);
    }
}
