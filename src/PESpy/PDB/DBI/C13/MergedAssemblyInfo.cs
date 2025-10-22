namespace PESpy.PDB
{
    //Type is made up
    public readonly struct MergedAssemblyInfo
    {
        public Timestamp Timestamp { get; }

        public int Index { get; }

        public bool IsPDB { get; }

        public VsVersionInfo VersionInfo { get; }

        //Don't know whether it's actually ANSI or UTF-8
        public Utf8String Name { get; }

        //Note that the struct size needs to be 32-bit aligned
        internal int StructSize =>
            (sizeof(int) + //Timestamp
            sizeof(int) + //Index
            VersionInfo.Length +
            Name.Length + 1 + 3) & ~3;

        public int Offset { get; }

        //VsVersionInfo allocates, so just eagerly read everything

        internal MergedAssemblyInfo(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            Timestamp = chunk.PeekUInt32(0);
            var index = chunk.PeekUInt32(4);

            //If the high bit is set, it's a PDB, which means you should clear the index in order to use it
            if ((index >> 31) != 0)
            {
                IsPDB = true;
                index &= 0x7fffffff; //Clear the high bit
            }
            else
                IsPDB = false;

            Index = (int) index;

            VersionInfo = new VsVersionInfo(chunk.Slice(8));
            Name = chunk.PeekUtf8NullTerminatedString(8 + VersionInfo.Length);
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
