namespace PESpy
{
    public struct BundleEncodedString
    {
        public int Length { get; }

        public string Value { get; }

        internal BundleEncodedString(IFileReader reader)
        {
            Length = reader.Read7BitEncodedInt32();

            //It's UTF8
            Value = reader.ReadUTF8String(Length);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
