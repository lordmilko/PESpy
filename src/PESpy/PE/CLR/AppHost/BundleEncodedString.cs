namespace PESpy
{
    public struct BundleEncodedString
    {
        public int Length { get; }

        public string Value { get; }

#if PEFAST

        internal BundleEncodedString(in MemoryChunk chunk)
        {
            //May need to eagerly read; don't know how many bytes the 7 bit encoded length may be
            throw new System.NotImplementedException();
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
            return Value;
        }
    }
}
