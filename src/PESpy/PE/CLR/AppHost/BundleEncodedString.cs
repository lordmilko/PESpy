namespace PESpy
{
    public struct BundleEncodedString
    {
        public int Length { get; }

        public FixedUtf8String Value { get; }

#if PEFAST
        internal BundleEncodedString(in MemoryChunk chunk, out int read)
        {
            //May need to eagerly read; don't know how many bytes the 7 bit encoded length may be
            Length = chunk.Peek7BitEncodedInt32(0, out var bytesRead);
            Value = chunk.PeekUtf8FixedLength(bytesRead, Length);

            read = bytesRead + Length;
        }
#else
        internal BundleEncodedString(IFileReader reader)
        {
            Length = reader.Read7BitEncodedInt32();

            //It's UTF8
            Value = reader.ReadUTF8String(Length);
        }
#endif

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
